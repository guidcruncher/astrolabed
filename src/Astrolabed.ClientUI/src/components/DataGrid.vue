<template>
  <div class="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden">
    <div class="overflow-x-auto">
      <table class="w-full text-left text-sm text-slate-300">
        <thead class="bg-slate-700/50 text-slate-400 uppercase text-xs">
          <tr>
            <th 
              v-for="col in columns" 
              :key="col.key" 
              class="p-4"
              :class="col.headerClass"
            >
              {{ col.label }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr 
            v-for="(item, index) in data" 
            :key="item.id ?? index" 
            @click="handleRowClick(item)"
            class="border-t border-slate-700 hover:bg-slate-750/50 transition-colors cursor-pointer"
          >
            <td 
              v-for="col in columns" 
              :key="col.key" 
              class="p-4"
              :class="col.cellClass"
            >
              <slot :name="getSlotName(col.key)" :row="item" :value="getNestedValue(item, col.key)">
                  {{ getNestedValue(item, col.key) ?? '-' }}
              </slot>
            </td>
          </tr>
          <tr v-if="!data || data.length === 0">
            <td :colspan="columns.length" class="p-4 text-center text-slate-500">
              <slot name="empty">No entries found.</slot>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="p-4 bg-slate-800 border-t border-slate-700 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-slate-400">
      <div class="flex items-center space-x-4">
        <div>
          Showing {{ totalCount === 0 ? 0 : ((page - 1) * pageSize) + 1 }} to {{ Math.min(page * pageSize, totalCount) }} of {{ totalCount }} entries
        </div>
        
        <div class="flex items-center space-x-2">
          <label for="pageSizeSelect" class="text-slate-400">Per page:</label>
          <select
            id="pageSizeSelect"
            :value="pageSize"
            @change="handlePageSizeChange"
            class="bg-slate-700 text-slate-200 border border-slate-600 rounded px-2 py-1 text-xs focus:outline-none focus:border-slate-500"
          >
            <option v-for="size in pageSizeOptions" :key="size" :value="size">
              {{ size }}
            </option>
          </select>
        </div>
      </div>

      <div class="flex items-center space-x-2">
        <button
          @click="changePage(page - 1)"
          :disabled="page <= 1"
          class="px-3 py-1 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed rounded text-slate-200"
        >
          Previous
        </button>
        <span class="px-2 py-1 text-slate-300">Page {{ page }} of {{ totalPages }}</span>
        <button
          @click="changePage(page + 1)"
          :disabled="page >= totalPages"
          class="px-3 py-1 bg-slate-700 hover:bg-slate-600 disabled:opacity-50 disabled:cursor-not-allowed rounded text-slate-200"
        >
          Next
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts" generic="T extends Record<string, any>">
import { computed, watch, onMounted } from 'vue'
import { type Column } from '../types/types'

const props = withDefaults(
  defineProps<{
    data: T[]
    columns: Column[]
    page?: number
    pageSize?: number
    totalCount: number
    pageSizeOptions?: number[]
  }>(),
  {
    page: 1,
    pageSize: 10,
    pageSizeOptions: () => [10, 20, 50, 100],
  }
)

const emit = defineEmits<{
  (e: 'loaded'): void
  (e: 'page-change', page: number): void
  (e: 'page-size-change', pageSize: number): void
  (e: 'row-select', row: T): void
  (e: 'update:page', page: number): void
  (e: 'update:pageSize', pageSize: number): void
}>()

const totalPages = computed(() => Math.ceil(props.totalCount / props.pageSize) || 1)

// Resolve deep property paths (e.g., 'f.g' -> item.f.g)
const getNestedValue = (obj: Record<string, any>, path: string) => {
  if (!obj || !path) return undefined
  return path.split('.').reduce((acc, key) => (acc && acc[key] !== undefined ? acc[key] : undefined), obj)
}

// Normalize slot names so paths like 'payload.questionName' become 'payload-questionName'
const getSlotName = (key: string) => {
  return `cell-${key.replace(/\./g, '-')}`
}

const changePage = (newPage: number) => {
  if (newPage >= 1 && newPage <= totalPages.value) {
    emit('update:page', newPage)
    emit('page-change', newPage)
  }
}

const handlePageSizeChange = (event: Event) => {
  const target = event.target as HTMLSelectElement
  const newSize = Number(target.value)
  emit('update:pageSize', newSize)
  emit('page-size-change', newSize)
}

const handleRowClick = (row: T) => {
  emit('row-select', row)
}

watch(
  () => props.data,
  () => {
    emit('loaded')
  },
  { deep: true }
)

onMounted(() => {
  emit('loaded')
})
</script>

