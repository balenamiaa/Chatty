using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Chatty.Client.Cache;
using Chatty.Client.Crypto;
using Chatty.Client.Exceptions;
using Chatty.Client.State;
using Chatty.Client.Storage;
using Chatty.Shared.Models.Attachments;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Services.Files;

/// <summary>
///     Implementation of file service with encryption support
/// </summary>
public class FileService(
    IHttpClientFactory httpClientFactory,
    ICacheService cache,
    IStateManager state,
    ICryptoService cryptoService,
    IDeviceManager deviceManager,
    ILogger<FileService> logger)
    : BaseService(httpClientFactory, logger, "FileService"), IFileService
{
    private readonly IStateManager _state = state;

    public async Task<AttachmentDto> GetAsync(Guid fileId, CancellationToken ct = default)
    {
        var response = await ExecuteWithPoliciesAsync(
            client => client.GetFromJsonAsync<AttachmentDto>($"api/files/{fileId}", ct));

        if (response is null)
        {
            throw new ApiException(
                "Failed to parse file response",
                HttpStatusCode.InternalServerError);
        }

        return response;
    }

    public async Task<AttachmentDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        // Create multipart form content
        using var formContent = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        formContent.Add(streamContent, "file", fileName);

        var response = await ExecuteWithPoliciesAsync(
            client => client.PostAsync("api/files", formContent, ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to upload file",
                response.StatusCode);
        }

        var file = await response.Content.ReadFromJsonAsync<AttachmentDto>(ct);
        if (file is null)
        {
            throw new ApiException(
                "Failed to parse file response",
                HttpStatusCode.InternalServerError);
        }

        return file;
    }

    public async Task<Stream> DownloadAsync(Guid fileId, CancellationToken ct = default)
    {
        var response = await ExecuteWithPoliciesAsync(
            client => client.GetAsync($"api/files/{fileId}/download", ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to download file",
                response.StatusCode);
        }

        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task DeleteAsync(Guid fileId, CancellationToken ct = default)
    {
        var response = await ExecuteWithPoliciesAsync(
            client => client.DeleteAsync($"api/files/{fileId}", ct));

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(
                "Failed to delete file",
                response.StatusCode);
        }
    }

    public async Task<string> GetUrlAsync(Guid fileId, CancellationToken ct = default)
    {
        var response = await ExecuteWithPoliciesAsync(
            client => client.GetFromJsonAsync<AttachmentUrlDto>($"api/files/{fileId}/url", ct));

        if (response?.Url is null)
        {
            throw new ApiException(
                "Failed to get file URL",
                HttpStatusCode.InternalServerError);
        }

        return response.Url;
    }

    public async Task<string> GetThumbnailUrlAsync(Guid fileId, CancellationToken ct = default)
    {
        var response = await ExecuteWithPoliciesAsync(
            client => client.GetFromJsonAsync<AttachmentUrlDto>($"api/files/{fileId}/thumbnail", ct));

        if (response?.Url is null)
        {
            throw new ApiException(
                "Failed to get thumbnail URL",
                HttpStatusCode.InternalServerError);
        }

        return response.Url;
    }
}
