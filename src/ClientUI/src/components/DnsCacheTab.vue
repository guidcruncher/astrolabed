<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAstrolabedApi, type DnsResponse } from '../composables/useAstrolabedApi'
import type { ColumnDef, PagedResult } from './types'

const { loading, getDnsCacheResponses, clearDnsCache } = useAstrolabedApi()

const cachePageNumber = ref<number>(1)
const cachePageSize = ref<number>(10)
const searchQuery = ref<string>('')

const pagedResult = ref<PagedResult<DnsResponse>>({
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
})

const dnsCacheColumns: ColumnDef<DnsResponse>[] = [
    { key: 'queryName', header: 'Query Name' },
    {
        key: 'queryType',
        header: 'Type',
        formatter: (row: DnsResponse) => `[${row.queryType}]`,
    },
    {
        key: 'responseCode',
        header: 'Status',
    },
    {
        key: 'answers',
        header: 'Answers',
        formatter: (row: DnsResponse) =>
            row.answers && row.answers.length > 0 ? row.answers.map((a) => a.data).join(', ') : '-',
    },
    {
        key: 'server',
        header: 'Source',
    },
]

const fetchCache = async (): Promise<void> => {
    try {
        const params = {
            pageNumber: cachePageNumber.value,
            pageSize: cachePageSize.value,
        }

        const result = await getDnsCacheResponses(params, searchQuery.value || undefined)
        pagedResult.value = result ?? {
            items: [],
            pageNumber: cachePageNumber.value,
            pageSize: cachePageSize.value,
            totalCount: 0,
        }
    } catch {
        pagedResult.value = {
            items: [],
            pageNumber: cachePageNumber.value,
            pageSize: cachePageSize.value,
            totalCount: 0,
        }
    }
}

const handleFlushCache = async (): Promise<void> => {
    try {
        await clearDnsCache()
        cachePageNumber.value = 1
        await fetchCache()
    } catch {
        // Error handling managed by API layer/toast notifications
    }
}

const handleSearch = (): void => {
    cachePageNumber.value = 1
    fetchCache()
}

const handlePageChange = (page: number): void => {
    cachePageNumber.value = page
    fetchCache()
}

const handlePageSizeChange = (size: number): void => {
    cachePageSize.value = size
    cachePageNumber.value = 1
    fetchCache()
}

onMounted(() => {
    fetchCache()
})
</script>

<template>
    <div class="wt-cache-tab">
        <div class="wt-cache-header">
            <div class="wt-cache-actions">
                <WhiptailButton :disabled="loading" @click="fetchCache">
                    {{ loading ? 'Refreshing...' : 'Refresh Cache' }}
                </WhiptailButton>
                <WhiptailButton :disabled="loading" variant="danger" @click="handleFlushCache">
                    Flush Cache
                </WhiptailButton>
            </div>
            <div class="wt-cache-search">
                <input
                    v-model="searchQuery"
                    type="text"
                    placeholder="Search domain or type..."
                    class="wt-input"
                    @keyup.enter="handleSearch"
                />
                <WhiptailButton :disabled="loading" @click="handleSearch"> Search </WhiptailButton>
            </div>
        </div>

        <WhiptailDataGrid
            :columns="dnsCacheColumns"
            :data="pagedResult"
            :loading="loading"
            @page-change="handlePageChange"
            @page-size-change="handlePageSizeChange"
        >
            <template #cell-responseCode="{ row }">
                <span :class="row.responseCode === 'NOERROR' ? 'wt-text-ok' : 'wt-text-err'">
                    {{ row.responseCode }}
                </span>
            </template>
            <template #cell-answers="{ row }">
                <div v-if="row.answers && row.answers.length > 0" class="wt-answers-list">
                    <span v-for="(answer, idx) in row.answers" :key="idx" class="wt-answer-tag">
                        {{ answer.data }}
                    </span>
                </div>
                <span v-else>-</span>
            </template>
        </WhiptailDataGrid>
    </div>
</template>

<style scoped>
.wt-cache-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
    gap: 12px;
}

.wt-cache-actions {
    display: flex;
    gap: 8px;
}

.wt-cache-search {
    display: flex;
    gap: 8px;
}

.wt-input {
    padding: 6px 12px;
    border-radius: 4px;
    border: 1px solid var(--wt-border-color, #ccc);
    background-color: var(--wt-bg-input, #fff);
    color: var(--wt-text-color, #000);
}

.wt-answers-list {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
}

.wt-answer-tag {
    background-color: var(--wt-bg-tag, #f0f0f0);
    padding: 2px 6px;
    border-radius: 3px;
    font-family: monospace;
    font-size: 0.85em;
}
</style>
