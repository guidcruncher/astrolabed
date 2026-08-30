export interface LineChartItem {
  label: string
  value: number
}

export type LegendPosition = 'left' | 'right' | 'top' | 'bottom'

export interface ActiveSegmentData {
  barId: string | number
  barLabel: string
  seriesId: string | number
  seriesLabel: string
  value: number
  percentage: number
  color: string
}

export interface StackedBarSeries {
  id: string | number
  label: string
  color: string
}

export interface StackedBarItem {
  id: string | number
  label: string
  values: Record<string | number, number>
}

export interface PieChartItem {
  id: string | number
  label: string
  value: number
  color: string
}

export interface ActiveSliceData extends PieChartItem {
  percentage: number
}

export type ModalAction = 'confirm' | 'cancel' | 'close' | string

export interface NavItem {
  icon?: string
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
