export interface TabItem {
  id: string;
  label: string;
}

export interface WhiptailOption {
    label: string
    value: string | number
}

export interface LoginCredentials {
    username: string
    password: string
}

export interface PagedResult<T> {
    items: T[]
    totalCount: number
    pageNumber: number
    pageSize: number
    totalPages?: number
    hasPreviousPage?: boolean
    hasNextPage?: boolean
}

export interface ColumnDef<T = Record<string, unknown>> {
    key: string
    header: string
    width?: string
    align?: 'left' | 'center' | 'right'
    formatter?: (row: T) => string | number
}
