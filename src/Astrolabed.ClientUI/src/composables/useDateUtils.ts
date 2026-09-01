import { ref } from 'vue'

export interface UseDateUtilsOptions {
  /**
   * BCP 47 language tag for formatting. Defaults to browser language.
   */
  locale?: string
}

/**
 * Vue 3 Composable providing date formatting utilities and local timezone state.
 */
export function useDateUtils(options: UseDateUtilsOptions = {}) {
  const currentLocale = ref<string>(
    options.locale || (typeof navigator !== 'undefined' ? navigator.language : 'en-US')
  )

  const timeZone = ref<string>('')

  /**
   * Retrieves the browser's target timezone identifier (e.g., "America/New_York", "Europe/London").
   */
  const getBrowserTimeZone = (): string => {
    try {
      return Intl.DateTimeFormat().resolvedOptions().timeZone
    } catch {
      return 'UTC'
    }
  }

  /**
   * Converts a Date object or ISO 8601 UTC date string into local browser time with format:
   * "ddd, dd MMM yyyy HH:mm:ss TZ" (e.g., "Tue, 01 Sep 2026 15:50:18 BST").
   * Enforces a 3-character month truncation.
   *
   * @param inputDate - Date object or ISO 8601 string (e.g., "2026-09-01T14:50:18.1434994+00:00")
   * @param overrideLocale - Optional locale string to override the default composition locale.
   * @returns Formatted date string in local time, or null if parsing fails.
   */
  const formatUtcToLocalBrowserTime = (
    inputDate: Date | string,
    overrideLocale?: string
  ): string | null => {
    if (!inputDate) {
      return null
    }

    const date = typeof inputDate === 'string' ? new Date(inputDate) : inputDate

    if (!(date instanceof Date) || isNaN(date.getTime())) {
      return null
    }

    const formatOptions: Intl.DateTimeFormatOptions = {
      weekday: 'short',
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      timeZoneName: 'short',
      hour12: false,
    }

    const targetLocale = overrideLocale || currentLocale.value
    const formatter = new Intl.DateTimeFormat(targetLocale, formatOptions)
    const parts = formatter.formatToParts(date)

    const partsMap = new Map<Intl.DateTimeFormatPartTypes, string>(
      parts.map((part) => [part.type, part.value])
    )

    const weekday = partsMap.get('weekday') ?? ''
    const day = partsMap.get('day') ?? ''

    // Explicitly enforce 3 characters for the month
    const rawMonth = partsMap.get('month') ?? ''
    const month = rawMonth.slice(0, 3)

    const year = partsMap.get('year') ?? ''
    const hour = partsMap.get('hour') ?? ''
    const minute = partsMap.get('minute') ?? ''
    const second = partsMap.get('second') ?? ''
    const timeZoneName = partsMap.get('timeZoneName') ?? ''

    return `${weekday}, ${day} ${month} ${year} ${hour}:${minute}:${second} ${timeZoneName}`
  }

  timeZone.value = getBrowserTimeZone()

  return {
    timeZone,
    currentLocale,
    formatUtcToLocalBrowserTime,
  }
}
