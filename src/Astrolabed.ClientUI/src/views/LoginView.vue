<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuth } from '../composables/useAuth'

const email = ref('')
const password = ref('')
const rememberMe = ref(false)
const errorMessage = ref<string | null>(null)
const isSubmitting = ref(false)

const router = useRouter()
const route = useRoute()
const { login } = useAuth()

async function handleLogin() {
  isSubmitting.value = true
  errorMessage.value = null

  try {
    await login(email.value, password.value, rememberMe.value)

    // Redirect to requested page or fallback to dashboard
    const redirectPath = (route.query.redirect as string) || '/app/dashboard'
    await router.push(redirectPath)
  } catch (err: unknown) {
    if (err instanceof Error) {
      errorMessage.value = err.message
    } else {
      errorMessage.value = 'An unexpected error occurred. Please try again.'
    }
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div
    class="min-h-screen bg-slate-950 flex flex-col justify-center py-12 sm:px-6 lg:px-8 font-sans text-slate-100"
  >
    <!-- Header / Branding -->
    <div class="sm:mx-auto sm:w-full sm:max-w-md text-center">
      <div
        class="mx-auto h-12 w-12 rounded-xl bg-slate-800 border border-slate-700 flex items-center justify-center shadow-lg"
      >
        <svg
          class="w-6 h-6 text-slate-300"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
          />
        </svg>
      </div>
      <h2 class="mt-6 text-3xl font-bold tracking-tight text-slate-100">Astrolabed</h2>
      <p class="mt-2 text-sm text-slate-400">Sign in to access</p>
    </div>

    <!-- Login Card -->
    <div class="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
      <div class="bg-slate-900 border border-slate-800 py-8 px-4 shadow-2xl rounded-xl sm:px-10">
        <form class="space-y-6" @submit.prevent="handleLogin">
          <!-- Error Alert -->
          <div
            v-if="errorMessage"
            class="rounded-lg bg-red-950/50 border border-red-800/60 p-4 text-sm text-red-300 flex items-start gap-3"
            role="alert"
          >
            <svg
              class="w-5 h-5 text-red-400 shrink-0 mt-0.5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <span>{{ errorMessage }}</span>
          </div>

          <!-- Email Input -->
          <div>
            <label for="email" class="block text-sm font-medium text-slate-300">
              Email address
            </label>
            <div class="mt-2">
              <input
                id="email"
                v-model="email"
                name="email"
                type="email"
                autocomplete="email"
                required
                placeholder="admin@astrolabed.local"
                class="block w-full rounded-lg border border-slate-700 bg-slate-950 px-3.5 py-2.5 text-slate-100 placeholder-slate-500 shadow-sm focus:border-slate-500 focus:outline-none focus:ring-2 focus:ring-slate-500/20 sm:text-sm transition-colors"
              />
            </div>
          </div>

          <!-- Password Input -->
          <div>
            <label for="password" class="block text-sm font-medium text-slate-300">
              Password
            </label>
            <div class="mt-2">
              <input
                id="password"
                v-model="password"
                name="password"
                type="password"
                autocomplete="current-password"
                required
                placeholder="••••••••"
                class="block w-full rounded-lg border border-slate-700 bg-slate-950 px-3.5 py-2.5 text-slate-100 placeholder-slate-500 shadow-sm focus:border-slate-500 focus:outline-none focus:ring-2 focus:ring-slate-500/20 sm:text-sm transition-colors"
              />
            </div>
          </div>

          <!-- Options Row -->
          <div class="flex items-center justify-between">
            <div class="flex items-center">
              <input
                id="remember-me"
                v-model="rememberMe"
                name="remember-me"
                type="checkbox"
                class="h-4 w-4 rounded border-slate-700 bg-slate-950 text-slate-600 focus:ring-slate-500 focus:ring-offset-slate-900"
              />
              <label for="remember-me" class="ml-2 block text-sm text-slate-400">
                Remember me
              </label>
            </div>
          </div>

          <!-- Submit Button -->
          <div>
            <button
              type="submit"
              :disabled="isSubmitting"
              class="flex w-full justify-center rounded-lg bg-slate-100 px-4 py-2.5 text-sm font-semibold text-slate-900 shadow-sm hover:bg-slate-200 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-slate-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <svg
                v-if="isSubmitting"
                class="animate-spin -ml-1 mr-2 h-4 w-4 text-slate-900"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  class="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  stroke-width="4"
                />
                <path
                  class="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
              <span>{{ isSubmitting ? 'Signing in...' : 'Sign in' }}</span>
            </button>
          </div>
        </form>
      </div>

      <!-- Footer Info -->
      <p class="mt-6 text-center text-xs text-slate-500">Astrolabed</p>
    </div>
  </div>
</template>
