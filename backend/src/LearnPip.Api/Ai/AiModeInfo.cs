// <copyright file="AiModeInfo.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearnPip.Api.Ai;

public sealed record AiModeInfo(string Mode, bool Available, string Recipient,
    string DataShared, int DailyQuota, int MaxInputBytes, int MaxImageBytes,
    bool NeedsUserKey);
