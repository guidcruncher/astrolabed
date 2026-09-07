// File: composables/useHeuristicScan.ts
import { ref } from 'vue'

export interface DomainAssessmentResult {
  domain: string
  isAdDomain: boolean
  totalScore: number
  triggeredRules: string[]
}

export function useHeuristicScan() {
  const assessment = ref<DomainAssessmentResult | null>(null)
  const isLoading = ref<boolean>(false)
  const error = ref<string | null>(null)

  const scanDomain = async (domain: string): Promise<DomainAssessmentResult | null> => {
    if (!domain.trim()) return null

    isLoading.value = true
    error.value = null

    try {
      // Replace with your actual backend endpoint (e.g., ASP.NET Core API)
      const response = await fetch(`/api/dns/scan?domain=${encodeURIComponent(domain.trim())}`)

      if (!response.ok) {
        throw new Error(`Scan failed with status: ${response.status} ${response.statusText}`)
      }

      const data: DomainAssessmentResult = await response.json()
      assessment.value = data
      return data
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'An unexpected error occurred.'
      error.value = message
      assessment.value = null
      return null
    } finally {
      isLoading.value = false
    }
  }

  const clear = () => {
    assessment.value = null
    error.value = null
    isLoading.value = false
  }

  return {
    assessment,
    isLoading,
    error,
    scanDomain,
    clear,
  }
}
