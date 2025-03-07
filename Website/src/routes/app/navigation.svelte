<script lang="ts">
  import { useAppContext } from '$lib/client/contexts/app'
  import { useNavigationContext } from '$lib/client/contexts/navigation'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { onMount } from 'svelte'
  import { derived, writable, type Readable } from 'svelte/store'

  const {
    label,
    onclick,
    icon,
    isActive
  }: {
    label: string
    onclick: () => Promise<void>
    icon: (isActive: boolean) => IconOptions
    isActive: Readable<boolean>
  } = $props()

  const { isMobile, isDesktop } = useAppContext()
  const { pushNavigation } = useNavigationContext()

  onMount(() => pushNavigation(content))

  const iconDisplay = derived(isActive, icon)

  const isLoading = writable(false)

  async function click(): Promise<void> {
    try {
      try {
        $isLoading = true
        await onclick()
      } catch (error: any) {
        throw error
      }
    } finally {
      $isLoading = false
    }
  }
</script>

{#snippet content()}
  <button
    onclick={() => click()}
    class:active={$isActive}
    class:mobile={$isMobile}
    class:desktop={$isDesktop}
  >
    {#if $isMobile && $isActive}
      <div class="indicator"></div>
    {/if}

    <div class="content">
      {#key $iconDisplay}
        <!-- {#if $isLoading}
        <LoadingSpinner size="1.5rem" />
        {:else}
          <Icon {...$iconDisplay} size="1.5em" />
        {/if} -->
          <Icon {...$iconDisplay} size="1.5em" />
      {/key}

      <p class="label">
        {label}
      </p>
    </div>
  </button>
{/snippet}

<style lang="scss">
  @use '../../global.scss' as *;

  button {
    -webkit-app-region: no-drag;

    display: flex;
    flex-direction: column;
    align-items: stretch;

    flex-basis: 0;

    line-height: 1em;
    padding: 8px 4px;
    margin: 4px;
    gap: 0;

    text-align: start;

    border: none;

    cursor: pointer;

    transition-duration: 150ms;
    transition-property: background-color, color;

    > div.content {
      align-items: center;
    }

    > div.indicator {
      @include force-size(&, 1px);

      background-color: var(--color-1);
    }
  }

  button.mobile {
    padding: 4px 8px;
    margin: 4px;

    flex-grow: 1;
  }

  button.desktop {
    border-radius: 8px;
  }

  button.active {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  button.active.mobile {
    background-color: transparent;
    color: inherit;

    > div.content {
      > p.label {
        font-weight: bolder;
      }
    }
  }
</style>
