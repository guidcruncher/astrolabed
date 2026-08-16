import { ref } from 'vue'
import { useRouter } from 'vue-router'
import type { LoginCredentials } from '../components/types'

interface User {
    username: string
    [key: string]: unknown
}

// Global reactive state shared across all component instances
const isAuthenticated = ref<boolean>(false)
const currentUser = ref<User | null>(null)
const isLoading = ref<boolean>(false)

export function useAuth() {
    const router = useRouter()

    /**
     * Verifies current session cookie with .NET backend endpoint
     */
    const checkAuth = async (): Promise<boolean> => {
        isLoading.value = true
        try {
            const response = await fetch('/api/v1/auth/me', {
                method: 'GET',
                headers: { Accept: 'application/json' },
                credentials: 'include',
            })

            if (response.ok) {
                const userData: User = await response.json()
                currentUser.value = userData
                isAuthenticated.value = true
                return true
            } else {
                currentUser.value = null
                isAuthenticated.value = false
                return false
            }
        } catch (error) {
            currentUser.value = null
            isAuthenticated.value = false
            return false
        } finally {
            isLoading.value = false
        }
    }

    /**
     * Submits credentials to .NET Login endpoint
     */
    const login = async (credentials: LoginCredentials): Promise<boolean> => {
        isLoading.value = true
        try {
            const response = await fetch('/api/v1/auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Accept: 'application/json',
                },
                credentials: 'include',
                body: JSON.stringify(credentials),
            })

            if (response.ok) {
                await checkAuth()
                return true
            } else {
                return false
            }
        } catch (error) {
            return false
        } finally {
            isLoading.value = false
        }
    }

    /**
     * Clears auth cookie on backend and resets client state
     */
    const logout = async (): Promise<void> => {
        isLoading.value = true
        try {
            await fetch('/api/v1/auth/logout', {
                method: 'POST',
                credentials: 'include',
            })
        } finally {
            currentUser.value = null
            isAuthenticated.value = false
            isLoading.value = false
            if (router) {
                router.push('/login')
            }
        }
    }

    return {
        isAuthenticated,
        currentUser,
        isLoading,
        checkAuth,
        login,
        logout,
    }
}
