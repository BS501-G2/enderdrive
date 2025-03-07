<script lang="ts">
  import Input from '$lib/client/ui/input.svelte'
  import { type Snippet } from 'svelte'
  import Overlay from '../../overlay.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { useAppContext } from '$lib/client/contexts/app'

  import { useClientContext } from '$lib/client/client'
  import { useFileBrowserContext } from '$lib/client/contexts/file-browser'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import { writable } from 'svelte/store'
  import type { FileResource } from '$lib/client/resource'

  const {
    parentFolder,
    ondismiss
  }: {
    parentFolder: FileResource
    ondismiss: () => void
  } = $props()
  const { isMobile } = useAppContext()
  const { server } = useClientContext()
  const { onFileId } = useFileBrowserContext()
  const { executeBackgroundTask } = useDashboardContext()

  const name = writable<string>('')

  async function onclick(
    event: MouseEvent & {
      currentTarget: EventTarget & HTMLButtonElement
    }
  ): Promise<void> {
    await executeBackgroundTask('name', async ({ setProgress, setMessage, setTitle }) => {
      setMessage('Creating folder...')
      const folder = await server.FolderCreate({
        FileId: parentFolder.Id,
        Name: $name
      })

      setMessage('Folder successfully created..')
      ondismiss()
      onFileId?.(event, folder.Id)
    })
  }
</script>

<Overlay {ondismiss}>
  {#snippet children(windowButtons: Snippet)}
    <div class="folder" class:mobile={$isMobile}>
      <div class="header">
        <div class="title">
          <p>Create Folder</p>
        </div>

        {@render windowButtons()}
      </div>

      <Separator horizontal />

      <div class="content">
        <div class="body">
          <p>
            Creating a new folder named
            <b class="folder-name">{$name}</b>
            will automatically redirect you inside it.
          </p>

          <Input id="folder-name" type="text" name="Folder Name" bind:value={$name} />
        </div>

        <Separator horizontal />

        <div class="footer">
          <Button {onclick}>
            <div class="button">
              <Icon icon="plus" thickness="solid" />
              <p>Create</p>
            </div>
          </Button>
        </div>
      </div>
    </div>
  {/snippet}
</Overlay>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.folder {
    background-color: var(--color-9);

    @include force-size(min(50dvw, 512px), &);

    > div.header {
      flex-direction: row;
      align-items: center;

      > div.title {
        flex-grow: 1;

        font-size: 1.2em;
        font-weight: bolder;

        padding: 0 8px;
      }
    }

    > div.content {
      padding: 8px;
      gap: 8px;

      > div.body {
        gap: 8px;
      }

      > div.footer {
        flex-direction: row;
        justify-content: flex-end;

        div.button {
          flex-direction: row;

          align-items: center;
          line-height: 1em;

          background-color: var(--color-1);
          color: var(--color-5);

          padding: 8px;
          gap: 8px;
        }
      }
    }
  }

  div.folder.mobile {
    @include force-size(min(75dvw, 512px), &);
  }

  b.folder-name {
    font-weight: bolder;

    text-wrap: wrap;
    word-wrap: break-word;
  }
</style>
