import { ref, computed } from 'vue'
import { apiFetch } from '@/services/api'

export interface User {
  id: string
  email: string
  displayName?: string
}

// Shared reactive state
const user = ref<User | null>(null)
const isLoading = ref<boolean>(true)

export function useAuth() {
  const isAuthenticated = computed(() => user.value !== null)

  /**
   * Verifies auth state against .NET 10 API on application startup.
   */
  async function fetchCurrentUser(): Promise<User | null> {
    isLoading.value = true
    try {
      user.value = await apiFetch<User>('/api/auth/me')
      return user.value
    } catch {
      user.value = null
      return null
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Authenticates user credentials via native fetch.
   */
  async function login(email: string, password: string, rememberMe = false): Promise<void> {
    user.value = await apiFetch<User>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password, rememberMe }),
    })
  }

  /**
   * Clears auth cookie on server and resets client state.
   */
  async function logout(): Promise<void> {
    try {
      await apiFetch('/api/auth/logout', { method: 'POST' })
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
