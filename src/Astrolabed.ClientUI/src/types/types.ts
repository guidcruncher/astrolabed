export interface NavItem {
  label: string
  shortLabel: string
  to: string
  exact?: boolean
}

export interface Column {
  key: string
  label: string
  headerClass?: string
  cellClass?: string
}
