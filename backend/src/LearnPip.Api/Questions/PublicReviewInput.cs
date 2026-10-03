// <copyright file="PublicReviewInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Moderationsprüfung einer eingereichten Fragenfassung.
/// </summary>
/// <param name="Decision">Die Moderationsentscheidung.</param>
/// <param name="CorrectnessChecked">Gibt an, ob die fachliche Richtigkeit geprüft wurde.</param>
/// <param name="ImageRightsChecked">Gibt an, ob die Bildrechte geprüft wurden.</param>
/// <param name="PersonalDataChecked">Gibt an, ob personenbezogene Daten geprüft wurden.</param>
/// <param name="DuplicateChecked">Gibt an, ob doppelte Inhalte geprüft wurden.</param>
/// <param name="Note">Die Begründung der Moderationsentscheidung.</param>
public sealed record PublicReviewInput(
        string Decision,
        bool CorrectnessChecked,
        bool ImageRightsChecked,
        bool PersonalDataChecked,
        bool DuplicateChecked,
        string Note);
