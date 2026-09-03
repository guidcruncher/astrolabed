import { ref, computed } from 'vue'
import { useFetchClient } from './useFetchClient'

export interface User {
  id: string
  email: string
  displayName?: string
}

// Shared reactive state across app instances
const user = ref<User | null>(null)
const isLoading = ref<boolean>(true)

export function useAuth() {
  const { request } = useFetchClient()
  const isAuthenticated = computed(() => user.value !== null)

  /**
   * Verifies auth state against .NET 10 API on application startup.
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
    }
  }

  /**
   * Authenticates user credentials via native fetch client.
   */
  async function login(email: string, password: string, rememberMe = false): Promise<void> {
    user.value = await request<User>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, rememberMe }),
    })
  }

  /**
   * Clears auth cookie on server and resets client state.
   */
  async function logout(): Promise<void> {
    try {
      await request<void>('/api/auth/logout', { method: 'POST' })
    } finally {
      user.value = null
    }
  }

  return {
    user: computed(() => user.value),
    isLoading: computed(() => isLoading.value),
    isAuthenticated,
    fetchCurrentUser,
    login,
    logout,
  }
}
