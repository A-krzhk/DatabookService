const DEFAULT_BASE =
  typeof window !== 'undefined' ? window.location.origin : 'http://localhost'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? DEFAULT_BASE

export function buildApiUrl(path, searchParams) {
  const url = new URL(path, API_BASE_URL)
  if (searchParams) {
    Object.entries(searchParams).forEach(([key, value]) => {
      if (value === undefined || value === null || value === '') return
      url.searchParams.set(key, value)
    })
  }
  return url.toString()
}

export async function apiRequest(path, options = {}) {
  const {
    method = 'GET',
    body,
    headers = {},
    apiKey,
    searchParams,
    signal,
  } = options

  if (!apiKey?.trim()) {
    throw new Error('API key is required for this request')
  }

  const url = buildApiUrl(path, searchParams)

  const finalHeaders = {
    ...headers,
    'X-API-Key': apiKey.trim(),
  }

  let requestBody = body
  if (body && !(body instanceof FormData) && !(body instanceof Blob)) {
    finalHeaders['Content-Type'] =
      finalHeaders['Content-Type'] ?? 'application/json'
    requestBody = JSON.stringify(body)
  }

  const response = await fetch(url, {
    method,
    headers: finalHeaders,
    body: requestBody,
    signal,
  })

  if (!response.ok) {
    const errorText = await response.text()
    throw new Error(errorText || `Request failed with status ${response.status}`)
  }

  if (response.status === 204) {
    return null
  }

  const contentType = response.headers.get('content-type') ?? ''

  if (contentType.includes('application/json')) {
    return response.json()
  }

  return response.text()
}
