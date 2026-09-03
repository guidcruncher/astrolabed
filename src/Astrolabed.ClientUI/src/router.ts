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
    component: DefaultLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('./views/DashboardView.vue'),
      },
      {
        path: 'demo',
        name: 'Demo',
        component: () => import('./views/DemoView.vue'),
      },
      {
        path: 'cache',
        name: 'Cache',
        component: () => import('./views/CacheListView.vue'),
      },
      {
        path: 'dhcp',
        name: 'DHCP',
        component: () => import('./views/DhcpView.vue'),
      },
      {
        path: 'dns',
        name: 'DNS',
        component: () => import('./views/DnsView.vue'),
      },
      {
        path: 'benchmark',
        name: 'Benchmark',
        component: () => import('./views/DnsBenchmarksView.vue'),
      },
      {
        path: 'dnsquery',
        name: 'DNSQuery',
        component: () => import('./views/DnsQueryView.vue'),
      },
      {
        path: 'network',
        name: 'Network',
        component: () => import('./views/NetworkView.vue'),
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

  if (isLoading.value) {
    await fetchCurrentUser()
  }

  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const guestOnly = to.matched.some((record) => record.meta.guestOnly)

  if (requiresAuth && !isAuthenticated.value) {
    return next({ name: 'login', query: { redirect: to.fullPath } })
  }

  if (guestOnly && isAuthenticated.value) {
    return next({ name: 'Dashboard' })
  }

  next()
})

export default router
