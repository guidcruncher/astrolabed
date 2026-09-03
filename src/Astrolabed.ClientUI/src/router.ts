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
    meta: { layout: LandingLayout, guestOnly: true, pageTitle: 'Home' },
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('./views/LoginView.vue'),
    meta: { layout: LandingLayout, guestOnly: true, pageTitle: 'Login' },
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
        meta: { layout: DefaultLayout, pageTitle: 'System Overview' },
      },
      {
        path: 'demo',
        name: 'Demo',
        component: () => import('./views/DemoView.vue'),
        meta: { layout: DefaultLayout, pageTitle: 'Demo Page' },
      },
      {
        path: 'cache',
        name: 'Cache',
        component: () => import('./views/CacheListView.vue'),
        meta: { layout: DefaultLayout, pageTitle: 'DNS Cache Entries' },
      },
      {
        path: 'dhcp',
        name: 'DHCP',
        component: () => import('./views/DhcpView.vue'),
        meta: { layout: DefaultLayout, pageTitle: 'DHCP Leases' },
      },
      {
        path: 'dns',
        name: 'DNS',
        component: () => import('./views/DnsView.vue'),
        meta: { layout: DefaultLayout, pageTitle: 'DNS Event Logs' },
      },
      {
        path: 'benchmark',
        name: 'Benchmark',
        component: () => import('./views/DnsBenchmarksView.vue'),
        meta: { layout: DefaultLayout, pageTitle: 'Public DNS Server Rankings' },
      },
      {
        path: 'dnsquery',
        name: 'DNSQuery',
        component: () => import('./views/DnsQueryView.vue'),
        meta: { layout: DefaultLayout, pageTitle: 'DNS Server Query' },
      },
      {
        path: 'network',
        name: 'Network',
        component: () => import('./views/NetworkView.vue'),
        meta: { layout: DefaultLayout, pageTitle: 'Discovered LAN Devices' },
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
