import { ref, computed } from 'vue'
import { useFetchClient } from './useFetchClient'

/**
 * Represents the authenticated user profile returned from the API.
 */
export interface User {
  id: string
  email: string
  displayName?: string
}

// Shared reactive state across app instances
const user = ref<User | null>(null)
const isLoading = ref<boolean>(false)
const isInitialized = ref<boolean>(false)

/**
 * Composable providing authentication state management and API actions.
 */
export function useAuth() {
  const { request } = useFetchClient()
  const isAuthenticated = computed(() => user.value !== null)

  /**
   * Verifies auth state against .NET 10 API on application startup.
   * Sets isInitialized to true once the initial request resolves.
   *
   * @returns The authenticated user or null if unauthenticated.
   */
  async function fetchCurrentUser(): Promise<User | null> {
    isLoading.value = true
    try {
      user.value = await request<User>('/api/auth/me')
      return user.value
    } catch {
      user.value = null
      return null
    } finally {
      isLoading.value = false
      isInitialized.value = true
    }
  }

  /**
   * Authenticates user credentials via native fetch client.
   *
   * @param email - User email address
   * @param password - User password
   * @param rememberMe - Whether to persist session cookie
   */
  async function login(email: string, password: string, rememberMe = false): Promise<void> {
    isLoading.value = true
    try {
      user.value = await request<User>('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password, rememberMe }),
      })
      isInitialized.value = true
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Clears auth cookie on server and resets client state.
   */
  async function logout(): Promise<void> {
    isLoading.value = true
    try {
      await request<void>('/api/auth/logout', { method: 'POST' })
    } finally {
      user.value = null
      isLoading.value = false
    }
  }

  return {
    user: computed(() => user.value),
    isLoading: computed(() => isLoading.value),
    isInitialized: computed(() => isInitialized.value),
    isAuthenticated,
    fetchCurrentUser,
    login,
    logout,
  }
}
