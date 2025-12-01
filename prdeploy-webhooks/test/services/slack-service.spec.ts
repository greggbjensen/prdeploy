import { expect, vi, beforeEach, describe, it } from 'vitest';
import { SlackService } from '@src/services';
import { ContainerHelper } from '@test/helpers';
import { container } from 'tsyringe';
import slack from 'slack';

const useMocks =
  !(process.env.SLACK_TOKEN && process.env.SLACK_EMAIL_DOMAIN && process.env.GITHUB_TOKEN) ||
  process.env.USE_MOCKS === 'true';

let fetchMock: any = null;

describe('postMessage', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();

    if (useMocks) {
      fetchMock = vi.spyOn(global, 'fetch').mockResolvedValue({
        status: 200,
        text: async () => 'Resulting text from message',
        ok: true,
        redirected: false,
        statusText: 'OK',
        headers: new Headers(),
        body: null,
        bodyUsed: false,
        url: '',
        type: 'default' as ResponseType,
        clone: function() { return this as Response; },
        arrayBuffer: async () => new ArrayBuffer(0),
        blob: async () => new Blob(),
        formData: async () => new FormData(),
        json: async () => ({}),
      } as Response);

      fetchMock.mockClear();
    }
  });

  it('posts message to channel', async () => {
    const service = container.resolve(SlackService);
    await service.postMessage(
      'deployUrl',
      `{
    "blocks": [
        {
            "type": "section",
            "text": {
                "type": "plain_text",
                "text": "Test message from PR Deploy GitHub App",
                "emoji": true
            }
        }
    ]
}`
    );
  });
});

describe('lookupUser', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();

    if (useMocks) {
      Object.assign(slack, {
        users: {
          lookupByEmail: vi.fn().mockResolvedValueOnce({
            ok: true,
            user: {
              id: 789745,
              name: 'dhivya'
            }
          })
        }
      });
    }
  });

  it('gets slack user by name', async () => {
    const service = container.resolve(SlackService);
    const name = 'dhivya-selvam';
    const user = await service.lookupUser({ name, email: '' } as any);

    expect(user).not.toBeNull();
    expect(user?.username).toBeTruthy();
    expect(user?.id).toBeTruthy();
  });

  it('gets slack user using email alias', async () => {
    const service = container.resolve(SlackService);
    const user = await service.lookupUser({ name: '', email: 'jdoe@myorg.com' } as any);

    expect(user).not.toBeNull();
    expect(user?.username).toBeTruthy();
    expect(user?.id).toBeTruthy();
  });

  it('gets slack user by email', async () => {
    const service = container.resolve(SlackService);
    const user = await service.lookupUser({
      name: 'Gregg B. Jensen',
      email: 'greggbjensen@users.noreply.github.com'
    } as any);

    expect(user).not.toBeNull();
    expect(user?.username).toBeTruthy();
    expect(user?.id).toBeTruthy();
  });

  it('returns null if name and email are empty', async () => {
    const service = container.resolve(SlackService);
    const user = await service.lookupUser({ name: '', email: '' } as any);

    expect(user).toBeNull();
  });
});

describe('translateGitHubMarkdown', () => {
  beforeEach(async () => {
    await ContainerHelper.registerDefaults();
  });

  it('converts HTML tables to text format', () => {
    const service = container.resolve(SlackService);
    const markdown = `
<table>
  <tr>
    <td>row 1 cell 1</td>
    <td>row 1 cell 2</td>
  </tr>
  <tr>
    <td>row 2 cell 1</td>
    <td>row 2 cell 2</td>
  </tr>
</table>
`;

    const result = service.translateGitHubMarkdown(markdown);
    expect(result).toContain('row 1 cell 1');
    expect(result).toContain('row 1 cell 2');
    expect(result).toContain('row 2 cell 1');
    expect(result).toContain('row 2 cell 2');
  });

  it('converts br tags to newlines', () => {
    const service = container.resolve(SlackService);
    const markdown = 'Line 1<br>Line 2<br/>Line 3';

    const result = service.translateGitHubMarkdown(markdown);
    expect(result).toContain('\n');
  });

  it('removes HTML comments', () => {
    const service = container.resolve(SlackService);
    const markdown = 'Text before <!-- This is a comment --> text after';

    const result = service.translateGitHubMarkdown(markdown);
    expect(result).not.toContain('<!--');
    expect(result).not.toContain('-->');
    expect(result).toContain('Text before');
    expect(result).toContain('text after');
  });

  it('strips other HTML tags but preserves content', () => {
    const service = container.resolve(SlackService);
    const markdown = '<div>Content inside div</div><p>Paragraph content</p>';

    const result = service.translateGitHubMarkdown(markdown);
    expect(result).toContain('Content inside div');
    expect(result).toContain('Paragraph content');
    expect(result).not.toContain('<div>');
    expect(result).not.toContain('</div>');
    expect(result).not.toContain('<p>');
    expect(result).not.toContain('</p>');
  });

  it('handles regular markdown correctly', () => {
    const service = container.resolve(SlackService);
    const markdown = '**Bold text** and *italic text* with [a link](https://example.com)';

    const result = service.translateGitHubMarkdown(markdown);
    expect(result).toBeTruthy();
  });

  it('handles nested HTML tables', () => {
    const service = container.resolve(SlackService);
    const markdown = `
<table>
  <tr>
    <td>Outer table cell</td>
  </tr>
</table>
`;

    const result = service.translateGitHubMarkdown(markdown);
    expect(result).toContain('Outer table cell');
  });

  it('handles empty markdown', () => {
    const service = container.resolve(SlackService);
    const result = service.translateGitHubMarkdown('');
    expect(result).toBe('');
  });

  it('handles markdown with mixed HTML and markdown', () => {
    const service = container.resolve(SlackService);
    const markdown = `
# Heading

Some **bold** text.

<table>
  <tr>
    <td>Table content</td>
  </tr>
</table>

More text with <br>line break.
`;

    const result = service.translateGitHubMarkdown(markdown);
    expect(result).toContain('Table content');
    expect(result).toContain('line break');
  });
});
