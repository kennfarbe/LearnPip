// <copyright file="IdentityConflictException.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LearnPip.Api.Identity;

/// <summary>
/// Meldet eine bereits anderweitig verknüpfte Anmeldeidentität.
/// </summary>
public sealed class IdentityConflictException : Exception;
