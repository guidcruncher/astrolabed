import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import LandingLayout from './layouts/LandingLayut.vue'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    component: () => import('./layouts/DefaultLayout.vue'),
    children: [
      {
        path: '/dashboard',
        name: 'Dashboard',
        component: () => import('./views/DashboardView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'demo',
        name: 'Demo',
        component: () => import('./views/DemoView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'cache',
        name: 'Cache',
        component: () => import('./views/CacheListView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'dhcp',
        name: 'DHCP',
        component: () => import('./views/DhcpView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'dns',
        name: 'DNS',
        component: () => import('./views/DnsView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'benchmark',
        name: 'Benchmark',
        component: () => import('./views/DnsBenchmarksView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'dnsquery',
        name: 'DNSQuery',
        component: () => import('./views/DnsQueryView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: 'network',
        name: 'Network',
        component: () => import('./views/NetworkView.vue'),
        meta: { requiresAuth: true },
      },
      {
        path: '/login',
        name: 'login',
        component: () => import('./views/LoginView.vue'),
        meta: { layout: LandingLayout, guestOnly: true },
      },
      {
        path: '/',
        name: 'home',
        component: () => import('./views/HomeView.vue'),
        meta: { layout: LandingLayout, guestOnly: true },
      },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to, from, next) => {
  const { isAuthenticated, fetchCurrentUser, isLoading } = useAuth()

  // Perform initial session check if loading
  if (isLoading.value) {
    await fetchCurrentUser()
  }

  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const guestOnly = to.matched.some((record) => record.meta.guestOnly)

  if (requiresAuth && !isAuthenticated.value) {
    return next({ name: 'login', query: { redirect: to.fullPath } })
  }

  if (guestOnly && isAuthenticated.value) {
    return next({ name: 'dashboard' })
  }

  next()
})

export default router
