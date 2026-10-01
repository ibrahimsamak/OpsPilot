// dev-tokens.example.ts  — copy to dev-tokens.ts (git-ignored) and paste real tokens
export interface DevUser {
  name: string;
  label: string;
  token: string;
}

export const DEV_USERS: DevUser[] = [
  { name: 'alice', label: 'alice — ops.viewer', token: 'PASTE_ALICE_TOKEN' },
  { name: 'bob', label: 'bob — ops.viewer + ops.operator', token: 'PASTE_BOB_TOKEN' },
  { name: 'carol', label: 'carol — ops.admin', token: 'PASTE_CAROL_TOKEN' },
  { name: 'mallory', label: 'mallory — no roles', token: 'PASTE_MALLORY_TOKEN' },
];
