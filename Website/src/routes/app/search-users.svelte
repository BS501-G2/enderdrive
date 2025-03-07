<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import type { Snippet } from 'svelte'
  import { type UserResource } from '$lib/client/resource'

  const { server } = useClientContext()

  const {
    searchString,
    card,
    ondismiss
  }: {
    searchString: string
    card: Snippet<[name: string, seeMore: () => void, snippet: Snippet]>
    ondismiss: () => void
  } = $props()
  import { goto } from '$app/navigation'
</script>

{@render card('Users', () => {}, main)}

{#snippet user(user: UserResource)}
  {#snippet buttonForeground(view: Snippet)}
    <div class="button">
      {@render view()}
    </div>
  {/snippet}

  <Button
    onclick={() => {
      goto(`/app/profile?id=${user.Id}`)
      ondismiss()
    }}
    foreground={buttonForeground}
  >
    <div class="icon">
      <Icon icon="user" thickness="solid" />
    </div>
    <div class="name">
      <p class="name">
        {user.FirstName}
        {user.MiddleName}
        {user.LastName}
      </p>
      <p class="username">
        @{user.Username}
      </p>
    </div>
  </Button>
{/snippet}

{#snippet main()}
  {#await server.GetUsers({ SearchString: searchString })}
  <div class="empty">
    <LoadingSpinner size="3em" />
  </div>
  {:then users}
  {#if users.length}
    {#each users as entry}
      {@render user(entry)}
    {/each}

    {:else}
    <div class="empty">
      <p>No results</p>
    </div>
  {/if}
  {/await}
{/snippet}

<style lang="scss">
  @use '../../global.scss' as *;

  div.empty {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }

  div.button {
    display: flex;
    text-align: left;
    flex-direction: row;
    align-items: center;

    flex-grow: 1;
    border: none;

    padding: 8px;
    gap: 8px;

    > div.name {
      flex-grow: 1;

      > p.name {
        font-size: 1.2em;
      }

      > p.username {
        font-size: 0.8em;
        font-weight: lighter;
      }
    }
  }
</style>
