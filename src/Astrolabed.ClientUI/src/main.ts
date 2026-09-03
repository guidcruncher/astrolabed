import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import './style.css'

import { useAuth } from './composables/useAuth'

// Listen for 401 events dispatched from API calls
window.addEventListener('auth:unauthorized', async () => {
  const { logout } = useAuth()
  await logout()
  window.location.href = '/login'
})

const app = createApp(App)
app.use(router)
app.mount('#app')
