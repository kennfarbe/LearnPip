"""Offline evaluation of the keywords used by the archived catalog schemas.

No remote references or package-provided schemas are evaluated. CI compares this
portable evaluator with the independent Draft 2020-12 jsonschema implementation.
New schema keywords fail closed until explicitly implemented here.
"""
from __future__ import annotations

from datetime import datetime
import json
from pathlib import Path
import re
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[1] / "schemas/catalog"
KEYWORDS = {"$schema", "$id", "$ref", "$defs", "title", "description", "type",
            "properties", "required", "additionalProperties", "const", "enum",
            "minLength", "maxLength", "pattern", "format", "items", "minItems",
            "maxItems", "uniqueItems", "minimum", "maximum", "oneOf", "allOf",
            "if", "then"}


def validate_document(value, version: str, name: str) -> None:
    """Use only a locally archived, explicitly supported schema version."""
    if version not in {"0.1.0", "0.2.0", "1.0.0"} or name not in {"manifest", "questions"}:
        raise ValueError("Unknown archived schema")
    folder = ROOT / version
    documents = {file: json.loads((folder / file).read_text())
                 for file in ("manifest.schema.json", "questions.schema.json")}

    def check(item, schema, document):
        if set(schema) - KEYWORDS:
            raise ValueError("Unsupported archived schema keyword")
        if "$ref" in schema:
            file, _, fragment = schema["$ref"].partition("#")
            if file and file not in documents:
                raise ValueError("Remote or unknown schema reference")
            source = documents[file] if file else document
            target = source
            for part in fragment.split("/")[1:]:
                target = target[part.replace("~1", "/").replace("~0", "~")]
            check(item, target, source)
        kind = schema.get("type")
        valid = {"object": isinstance(item, dict), "array": isinstance(item, list),
                 "string": isinstance(item, str), "integer": type(item) is int}
        if kind and not valid.get(kind, False):
            raise ValueError("Schema type: " + kind)
        if "const" in schema and item != schema["const"]:
            raise ValueError("Schema constant mismatch")
        if "enum" in schema and item not in schema["enum"]:
            raise ValueError("Schema enum mismatch")
        if isinstance(item, dict):
            properties = schema.get("properties", {})
            if not set(schema.get("required", [])) <= item.keys():
                raise ValueError("Missing schema field")
            if schema.get("additionalProperties") is False and item.keys() - properties.keys():
                raise ValueError("Unknown schema field")
            for key in item.keys() & properties.keys():
                check(item[key], properties[key], document)
        if isinstance(item, str):
            if not schema.get("minLength", 0) <= len(item) <= schema.get("maxLength", len(item)):
                raise ValueError("Schema text length")
            if "pattern" in schema and not re.search(schema["pattern"], item):
                raise ValueError("Schema text pattern")
            if schema.get("format") == "uri" and not urlsplit(item).scheme:
                raise ValueError("Schema absolute URI required")
            if schema.get("format") == "date-time":
                if not re.fullmatch(r"\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(\.\d+)?([Zz]|[+-]\d{2}:\d{2})", item):
                    raise ValueError("Schema timestamp required")
                datetime.fromisoformat(item.upper().replace("Z", "+00:00"))
        if isinstance(item, list):
            if not schema.get("minItems", 0) <= len(item) <= schema.get("maxItems", len(item)):
                raise ValueError("Schema array length")
            if schema.get("uniqueItems") and len({json.dumps(part, sort_keys=True) for part in item}) != len(item):
                raise ValueError("Schema unique array items required")
            if "items" in schema:
                for part in item:
                    check(part, schema["items"], document)
        if type(item) in (int, float):
            if item < schema.get("minimum", item) or item > schema.get("maximum", item):
                raise ValueError("Schema number range")

        def matches(branch):
            try:
                check(item, branch, document)
                return True
            except ValueError:
                return False

        if "oneOf" in schema and sum(matches(branch) for branch in schema["oneOf"]) != 1:
            raise ValueError("Schema oneOf mismatch")
        for branch in schema.get("allOf", []):
            check(item, branch, document)
        if "if" in schema and matches(schema["if"]) and "then" in schema:
            check(item, schema["then"], document)

    schema = documents[name + ".schema.json"]
    check(value, schema, schema)
