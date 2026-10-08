"""Exchange only a local ZIP between two freshly migrated, isolated CI stacks."""
import io
import json
from pathlib import Path
import sys
import urllib.request
import uuid
import zipfile


def request(base, path, method='GET', payload=None, token=None, raw=None, content_type=None):
    headers = {}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    if payload is not None:
        raw = json.dumps(payload).encode()
        content_type = 'application/json'
    if content_type:
        headers['Content-Type'] = content_type
    with urllib.request.urlopen(urllib.request.Request(base + '/api/v1/' + path, data=raw, headers=headers, method=method), timeout=60) as response:
        data = response.read()
        return json.loads(data)['data'] if response.headers.get_content_type() == 'application/json' else data


def multipart(fields, file, name, media_type):
    boundary = 'learnpip-synthetic-' + uuid.uuid4().hex
    body = bytearray()
    for key, value in fields.items():
        body.extend(f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode())
    body.extend(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{name}"\r\nContent-Type: {media_type}\r\n\r\n'.encode())
    body.extend(file)
    body.extend(f'\r\n--{boundary}--\r\n'.encode())
    return bytes(body), 'multipart/form-data; boundary=' + boundary


a, b = sys.argv[1:]
first = request(a, 'auth/pseudonymous', 'POST', {})
second = request(b, 'auth/pseudonymous', 'POST', {})
at, bt = first['session']['token'], second['session']['token']
fixture = Path(__file__).resolve().parents[1] / 'fixtures/catalog/0.2.0/golden.zip'
with zipfile.ZipFile(fixture) as archive:
    png = archive.read(next(name for name in archive.namelist() if name.endswith('.png')))
raw, kind = multipart({'altText': 'Synthetisches Testbild'}, png, 'synthetic.png', 'image/png')
image = request(a, 'media/', 'POST', token=at, raw=raw, content_type=kind)['id']
ids = []
for index in range(5):
    content = {
        'selectionMode': 'single', 'subject': 'Technik', 'topic': 'Synthetisch',
        'language': 'de', 'source': 'Eigenes Original', 'license': 'LicenseRef-Private',
        'prompt': [{'kind': 'text', 'text': f'Synthetische Frage {index}', 'mediaId': None}],
        'explanation': [{'kind': 'text', 'text': 'Erklärung mit Größe und Umlauten', 'mediaId': None}],
        'answers': [{'isCorrect': True, 'blocks': [{'kind': 'text', 'text': 'Richtige Lösung', 'mediaId': None}]},
                    {'isCorrect': False, 'blocks': [{'kind': 'text', 'text': 'Andere Antwort', 'mediaId': None}]}],
    }
    if index == 0:
        for blocks in [content['prompt'], content['explanation'], content['answers'][0]['blocks']]:
            blocks.append({'kind': 'image', 'text': None, 'mediaId': image})
    ids.append(request(a, 'questions/drafts', 'POST', {'content': content, 'catalogId': None}, at)['questionId'])

license = {'id': 'LicenseRef-Private', 'holder': 'Synthetischer Testautor', 'attribution': 'Eigene Frage und eigene Grafik'}
proof = request(a, 'catalog-rights/' + ids[0], token=at)
evidence = {'license': license, 'provenance': {'kind': 'original'}}
request(a, 'catalog-rights/' + ids[0], 'PUT', {'contentSha256': proof['contentSha256'], 'rights': {**evidence, 'metadata': {'age_band': 'Klasse 9', 'difficulty': 'easy', 'topics': ['Biologie / Grundlagen', 'Klasse 9']}, 'media': {image: evidence}}}, at)
export = {'questionIds': ids[:3], 'title': 'Unabhängige Teilauswahl', 'publisher': 'Synthetischer Testautor',
          'questionLicense': license, 'imageLicense': license, 'licenseNotice': 'LicenseRef-Private: berechtigte private Weitergabe.',
          'rightsConfirmed': False, 'previewSha256': None}
preview = request(a, 'catalog-exports/preview', 'POST', export, at)
assert preview['questionCount'] == 3 and preview['mediaCount'] == 1
assert not preview['communityEligible']
export.update(rightsConfirmed=True, previewSha256=preview['previewSha256'])
package = request(a, 'catalog-exports/download', 'POST', export, at)
with zipfile.ZipFile(io.BytesIO(package)) as archive:
    assert {'manifest.json', 'questions.json', 'LICENSES.md', 'NOTICE', 'ATTRIBUTION'} <= set(archive.namelist())
    content = '\n'.join(archive.read(name).decode() for name in archive.namelist() if not name.startswith('media/'))
    for secret in [first['accountId'], second['accountId'], first['recoverySecret'], at, bt]:
        assert secret not in content
    assert 'Synthetische Frage 3' not in content and 'Synthetische Frage 4' not in content
    questions = json.loads(archive.read('questions.json'))['questions']
    assert len(questions) == 3
    assert {item['id'] for item in questions} == {'learnpip-question:' + item.replace('-', '') for item in ids[:3]}

raw, kind = multipart({}, package, 'exchange.zip', 'application/zip')
import_preview = request(b, 'catalog-packages/preview', 'POST', token=bt, raw=raw, content_type=kind)
assert import_preview['questionCount'] == 3 and import_preview['state'] == 'new'
raw, kind = multipart({'rightsConfirmed': 'true', 'archiveSha256': import_preview['archiveSha256']}, package, 'exchange.zip', 'application/zip')
request(b, 'catalog-packages/import', 'POST', token=bt, raw=raw, content_type=kind)
assert request(b, 'catalog-packages/import', 'POST', token=bt, raw=raw, content_type=kind)['alreadyImported']
imported = request(b, 'catalog-exports/questions', token=bt)
assert len(imported) == 3
classified = next(item for item in imported if item['prompt'] == 'Synthetische Frage 0')
assert classified['audience'] == 'Klasse 9' and classified['difficulty'] == 'easy'
assert 'Biologie / Grundlagen' in classified['tags']
local = next(item for item in imported if item['prompt'] == 'Synthetische Frage 0')['id']
version = request(b, f'questions/{local}/versions/1', token=bt)
assert version['visibility'] == 'private' and version['license'] == 'LicenseRef-Private'
assert any(block['kind'] == 'image' for block in version['explanation'])
assert any(block['kind'] == 'image' for block in version['answers'][0]['blocks'])
print('PASS: two independent fresh Docker stacks; five questions, image, selective local ZIP, rights metadata, private import and duplicate-free reimport.')
