<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import WhiptailLoginBox from '../components/WhiptailLoginBox.vue'
import type { LoginCredentials } from '../components/types'

const router = useRouter()
const route = useRoute()
const { login, isLoading } = useAuth()
const errorMessage = ref<string>('')

const handleLogin = async (credentials: LoginCredentials) => {
    errorMessage.value = ''
    const success = await login(credentials)

    if (success) {
        const redirectPath = (route.query.redirect as string) || '/dashboard'
        router.push(redirectPath)
    } else {
        errorMessage.value = 'Invalid username or password'
    }
}
</script>

<template>
    <div class="middle logo">
        <WhiptailLoginBox
            title="Astrolabed Authentication"
            :show-cancel="false"
            @submit="handleLogin"
        />
        <div v-if="errorMessage" class="error-msg">
            {{ errorMessage }}
        </div>
        <div v-if="isLoading" class="loading-msg">Authenticating...</div>
    </div>
</template>

<style scoped>
.error-msg {
    color: #ff0000;
    text-align: center;
    font-family: monospace;
    margin-top: 10px;
}
.loading-msg {
    color: #ffff00;
    text-align: center;
    font-family: monospace;
    margin-top: 10px;
}
</style>
