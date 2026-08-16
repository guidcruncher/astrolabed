<script setup lang="ts" generic="T extends Record<string, any>">
import { computed } from 'vue'
import WhiptailButton from './WhiptailButton.vue'
import WhiptailSelect from './WhiptailSelect.vue'
import type { ColumnDef, PagedResult, WhiptailOption } from './types'

interface Props {
    data: PagedResult<T>
    columns: ColumnDef<T>[]
    pageSize?: number
    pageSizeOptions?: number[]
    loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    pageSize: 10,
    pageSizeOptions: () => [5, 10, 20, 50],
    loading: false,
})

const emit = defineEmits<{
    (e: 'page-change', page: number): void
    (e: 'page-size-change', size: number): void
    (e: 'row-click', row: T): void
}>()

// Active page size priority: prop -> payload size -> fallback default (10)
const activePageSize = computed(() => props.pageSize || props.data.pageSize || 10)

// Total pages calculation based on total count and active page size
const totalPages = computed(() => {
    const size = activePageSize.value
    return size > 0 ? Math.ceil(props.data.totalCount / size) : 0
})

// Ensures items array strictly respects activePageSize when client-side pagination occurs
const displayedItems = computed(() => {
    if (!props.data.items) return []
    // If backend already paged the array to match activePageSize, return as-is
    if (props.data.items.length <= activePageSize.value) {
        return props.data.items
    }
    // If full unpaged list was passed, slice to current page view
    const start = (props.data.pageNumber - 1) * activePageSize.value
    return props.data.items.slice(start, start + activePageSize.value)
})

const hasPreviousPage = computed(() => {
    if (props.data.hasPreviousPage !== undefined) return props.data.hasPreviousPage
    return props.data.pageNumber > 1
})

const hasNextPage = computed(() => {
    if (props.data.hasNextPage !== undefined) return props.data.hasNextPage
    return props.data.pageNumber < totalPages.value
})

const pageSizeSelectOptions = computed<WhiptailOption[]>(() =>
    props.pageSizeOptions.map((size) => ({
        label: `${size} / page`,
        value: size,
    })),
)

const goToPage = (page: number): void => {
    if (page >= 1 && page <= totalPages.value && page !== props.data.pageNumber) {
        emit('page-change', page)
    }
}

const handlePageSizeChange = (val: string | number | null): void => {
    if (val !== null) {
        emit('page-size-change', Number(val))
    }
}

const getCellValue = (row: T, col: ColumnDef<T>): string | number => {
    if (col.formatter) return col.formatter(row)
    return row[col.key] ?? ''
}
</script>

<template>
    <div class="wt-datagrid-container">
        <!-- Grid Table -->
        <div class="wt-datagrid-scroll">
            <table class="wt-datagrid">
                <thead>
                    <tr>
                        <th
                            v-for="col in columns"
                            :key="col.key"
                            :style="{ width: col.width, textAlign: col.align || 'left' }"
                        >
                            {{ col.header }}
                        </th>
                    </tr>
                </thead>
                <tbody>
                    <!-- Loading Row -->
                    <tr v-if="loading">
                        <td :colspan="columns.length" class="wt-grid-message">
                            [ LOADING DATA... ]
                        </td>
                    </tr>

                    <!-- Empty State -->
                    <tr v-else-if="!displayedItems || displayedItems.length === 0">
                        <td :colspan="columns.length" class="wt-grid-message">
                            [ NO RECORDS FOUND ]
                        </td>
                    </tr>

                    <!-- Data Rows -->
                    <tr
                        v-for="(row, rowIndex) in displayedItems"
                        v-else
                        :key="rowIndex"
                        class="wt-grid-row"
                        @click="emit('row-click', row)"
                    >
                        <td
                            v-for="col in columns"
                            :key="col.key"
                            :style="{ textAlign: col.align || 'left' }"
                        >
                            <slot
                                :name="`cell-${col.key}`"
                                :row="row"
                                :value="getCellValue(row, col)"
                            >
                                {{ getCellValue(row, col) }}
                            </slot>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Pager Controls -->
        <div class="wt-datagrid-pager">
            <div class="wt-pager-info">
                Total: <span class="wt-highlight">{{ data.totalCount }}</span> | Page
                <span class="wt-highlight">{{ data.pageNumber }}</span> of
                <span class="wt-highlight">{{ totalPages }}</span>
            </div>

            <!-- Page Size Selector -->
            <div class="wt-pager-size">
                <WhiptailSelect
                    :model-value="activePageSize"
                    :options="pageSizeSelectOptions"
                    @change="handlePageSizeChange"
                />
            </div>

            <div class="wt-pager-actions">
                <WhiptailButton :disabled="!hasPreviousPage || loading" @click="goToPage(1)">
                    &lt;&lt; First
                </WhiptailButton>

                <WhiptailButton
                    :disabled="!hasPreviousPage || loading"
                    @click="goToPage(data.pageNumber - 1)"
                >
                    &lt; Prev
                </WhiptailButton>

                <WhiptailButton
                    :disabled="!hasNextPage || loading"
                    @click="goToPage(data.pageNumber + 1)"
                >
                    Next &gt;
                </WhiptailButton>

                <WhiptailButton :disabled="!hasNextPage || loading" @click="goToPage(totalPages)">
                    Last &gt;&gt;
                </WhiptailButton>
            </div>
        </div>
    </div>
</template>

<style scoped>
.wt-datagrid-container {
    display: flex;
    flex-direction: column;
    background-color: #000000;
    border: 1px solid #5f5f5f;
    font-family: inherit;
    width: 100%;
}

.wt-datagrid-scroll {
    overflow-x: auto;
}

.wt-datagrid {
    width: 100%;
    border-collapse: collapse;
    color: #e0e0e0;
}

.wt-datagrid th {
    background-color: #005f87;
    color: #ffffff;
    padding: 6px 10px;
    font-weight: bold;
    border-bottom: 1px solid #5f5f5f;
    white-space: nowrap;
}

.wt-datagrid td {
    padding: 6px 10px;
    border-bottom: 1px solid #3a3a3a;
    white-space: nowrap;
}

.wt-grid-row {
    cursor: pointer;
}

.wt-grid-row:hover {
    background-color: #333333;
}

.wt-grid-message {
    text-align: center;
    color: #ffff00;
    padding: 16px !important;
}

.wt-datagrid-pager {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 6px 10px;
    background-color: #1b1b1b;
    border-top: 1px solid #5f5f5f;
    flex-wrap: wrap;
    gap: 8px;
}

.wt-pager-info {
    font-size: 0.9rem;
    color: #c0c0c0;
}

.wt-highlight {
    color: #ffff00;
    font-weight: bold;
}

.wt-pager-size {
    width: 120px;
}

.wt-pager-actions {
    display: flex;
    gap: 4px;
}

.wt-pager-actions :deep(.wt-btn) {
    min-width: unset;
    padding: 2px 8px;
    font-size: 0.85rem;
}
</style>
