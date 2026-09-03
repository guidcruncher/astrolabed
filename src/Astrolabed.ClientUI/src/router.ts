import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import LandingLayout from './layouts/LandingLayout.vue'
import DefaultLayout from './layouts/DefaultLayout.vue'
import { useAuth } from './composables/useAuth'

const routes: Array<RouteRecordRaw> = [
  // Standalone Public Routes (Uses LandingLayout via meta)
  {
    path: '/',
    name: 'home',
    component: () => import('./views/HomeView.vue'),
    meta: { layout: LandingLayout, guestOnly: true },
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('./views/LoginView.vue'),
    meta: { layout: LandingLayout, guestOnly: true },
  },

  // Authenticated Dashboard Routes (Uses DefaultLayout via meta or nested view)
  {
    path: '/app',
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('./views/DashboardView.vue'),
        meta: { layout: DefaultLayout },
      },
      {
        path: 'demo',
        name: 'Demo',
        component: () => import('./views/DemoView.vue'),
        meta: { layout: DefaultLayout },
      },
      {
        path: 'cache',
        name: 'Cache',
        component: () => import('./views/CacheListView.vue'),
        meta: { layout: DefaultLayout },
      },
      {
        path: 'dhcp',
        name: 'DHCP',
        component: () => import('./views/DhcpView.vue'),
        meta: { layout: DefaultLayout },
      },
      {
        path: 'dns',
        name: 'DNS',
        component: () => import('./views/DnsView.vue'),
        meta: { layout: DefaultLayout },
      },
      {
        path: 'benchmark',
        name: 'Benchmark',
        component: () => import('./views/DnsBenchmarksView.vue'),
        meta: { layout: DefaultLayout },
      },
      {
        path: 'dnsquery',
        name: 'DNSQuery',
        component: () => import('./views/DnsQueryView.vue'),
        meta: { layout: DefaultLayout },
      },
      {
        path: 'network',
        name: 'Network',
        component: () => import('./views/NetworkView.vue'),
        meta: { layout: DefaultLayout },
      },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

// router/index.ts
router.beforeEach(async (to, from, next) => {
  const { isAuthenticated, fetchCurrentUser, isLoading, user } = useAuth()

  // Wait for the initial session check if loading
  if (user.value === null && isLoading.value) {
    await fetchCurrentUser()
  }

  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)

  if (requiresAuth && !isAuthenticated.value) {
    return next({ name: 'login', query: { redirect: to.fullPath } })
  }

  next()
})

export default router
