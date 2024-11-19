using System.Net.Http.Json;

using Chatty.Client.Exceptions;

namespace Chatty.Client.Http;

/// <summary>
///     Extension methods for HttpClient
/// </summary>
internal static class HttpClientExtensions
{
    /// <summary>
    ///     Ensures the response was successful and throws appropriate exceptions if not
    /// </summary>
    public static async Task EnsureSuccessAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ApiErrorResponse? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        }
        catch
        {
            // If we can't parse the error response, just throw a generic exception
            throw ApiException.FromStatusCode(response.StatusCode);
        }

        if (error is null)
        {
            throw ApiException.FromStatusCode(response.StatusCode);
        }

        throw new ApiException(
            error.Message,
            response.StatusCode,
            error.Code);
    }

    /// <summary>
    ///     Sends a GET request and ensures the response was successful
    /// </summary>
    public static async Task<HttpResponseMessage> GetAndEnsureSuccessAsync(
        this HttpClient client,
        string requestUri,
        CancellationToken ct = default)
    {
        var response = await client.GetAsync(requestUri, ct);
        await response.EnsureSuccessAsync();
        return response;
    }

    /// <summary>
    ///     Sends a POST request and ensures the response was successful
    /// </summary>
    public static async Task<HttpResponseMessage> PostAndEnsureSuccessAsync(
        this HttpClient client,
        string requestUri,
        HttpContent? content = null,
        CancellationToken ct = default)
    {
        var response = await client.PostAsync(requestUri, content, ct);
        await response.EnsureSuccessAsync();
        return response;
    }

    /// <summary>
    ///     Sends a PUT request and ensures the response was successful
    /// </summary>
    public static async Task<HttpResponseMessage> PutAndEnsureSuccessAsync(
        this HttpClient client,
        string requestUri,
        HttpContent? content = null,
        CancellationToken ct = default)
    {
        var response = await client.PutAsync(requestUri, content, ct);
        await response.EnsureSuccessAsync();
        return response;
    }

    /// <summary>
    ///     Sends a DELETE request and ensures the response was successful
    /// </summary>
    public static async Task<HttpResponseMessage> DeleteAndEnsureSuccessAsync(
        this HttpClient client,
        string requestUri,
        CancellationToken ct = default)
    {
        var response = await client.DeleteAsync(requestUri, ct);
        await response.EnsureSuccessAsync();
        return response;
    }

    /// <summary>
    ///     Sends a GET request and deserializes the response
    /// </summary>
    public static async Task<T?> GetFromJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        CancellationToken ct = default)
    {
        var response = await client.GetAndEnsureSuccessAsync(requestUri, ct);
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    /// <summary>
    ///     Sends a POST request and deserializes the response
    /// </summary>
    public static async Task<TResponse?> PostAsJsonAsync<TRequest, TResponse>(
        this HttpClient client,
        string requestUri,
        TRequest request,
        CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync(requestUri, request, ct);
        await response.EnsureSuccessAsync();
        return await response.Content.ReadFromJsonAsync<TResponse>(ct);
    }

    /// <summary>
    ///     Sends a PUT request and deserializes the response
    /// </summary>
    public static async Task<TResponse?> PutAsJsonAsync<TRequest, TResponse>(
        this HttpClient client,
        string requestUri,
        TRequest request,
        CancellationToken ct = default)
    {
        var response = await client.PutAsJsonAsync(requestUri, request, ct);
        await response.EnsureSuccessAsync();
        return await response.Content.ReadFromJsonAsync<TResponse>(ct);
    }
}
