/**
 * Mock for slackify-markdown package to handle ES module issues in Jest
 */
export const slackifyMarkdown = (markdown: string): string => {
  // Basic markdown to Slack conversion for testing
  // This is a simplified version - the real package does more
  return markdown
    .replace(/\*\*(.+?)\*\*/g, '*$1*') // Bold
    .replace(/\*(.+?)\*/g, '_$1_') // Italic
    .replace(/\[(.+?)\]\((.+?)\)/g, '<$2|$1>') // Links
    .replace(/`(.+?)`/g, '`$1`'); // Code
};
