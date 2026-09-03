import { ref } from 'vue'
import type { ProblemDetails } from '../types/api'

const apiBaseUrl = ref<string>('')

export function useFetchClient() {
  const loading = ref<boolean>(false)
  const error = ref<string | null>(null)

  const request = async <T>(endpoint: string, options: RequestInit = {}): Promise<T> => {
    loading.value = true
    error.value = null
    try {
      const baseUrl = apiBaseUrl.value.endsWith('/')
        ? apiBaseUrl.value.slice(0, -1)
        : apiBaseUrl.value
      const path = endpoint.startsWith('/') ? endpoint : `/${endpoint}`
      const url = `${baseUrl}${path}`

      const response = await fetch(url, {
        // CRITICAL: Send and process HttpOnly cookies across origins/requests
        credentials: 'include',
        ...options,
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
          ...options.headers,
        },
      })

      if (response.status === 401) {
        // Dispatch event for global auth handling (e.g., redirecting to login via router)
        window.dispatchEvent(new CustomEvent('auth:unauthorized'))
      }

      if (!response.ok) {
        const problem: ProblemDetails | null = await response.json().catch(() => null)
        throw new Error(
          problem?.detail || problem?.title || `HTTP ${response.status}: ${response.statusText}`
        )
      }

      if (response.status === 204) {
        return null as unknown as T
      }

      return await response.json()
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'An unexpected error occurred.'
      error.value = message
      throw err
    } finally {
      loading.value = false
    }
  }

  return {
    apiBaseUrl,
    loading,
    error,
    request,
  }
}
