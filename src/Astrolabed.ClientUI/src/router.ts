import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    component: () => import('./layouts/DefaultLayout.vue'),
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: () => import('./views/DashboardView.vue'),
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

export default router
