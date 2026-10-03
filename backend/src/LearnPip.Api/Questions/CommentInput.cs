// <copyright file="CommentInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Anfrage zum Verfassen eines Kommentars.
/// </summary>
/// <param name="Text">Der Textinhalt, sofern vorhanden.</param>
public sealed record CommentInput(string Text);
