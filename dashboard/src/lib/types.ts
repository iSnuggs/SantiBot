export interface User {
  userId: string;
  username: string;
  avatar: string;
}

export interface Guild {
  id: string;
  name: string;
  icon: string | null;
  iconUrl: string | null;
  owner: boolean;
  canManage: boolean;
}

export interface StarboardConfig {
  enabled: boolean;
  channelId?: string;
  threshold?: number;
  emoji?: string;
  allowSelfStar?: boolean;
}

export interface LoggingConfig {
  enabled: boolean;
  messageUpdated?: string;
  messageDeleted?: string;
  userJoined?: string;
  userLeft?: string;
  userBanned?: string;
  userUnbanned?: string;
  userUpdated?: string;
  channelCreated?: string;
  channelDestroyed?: string;
  channelUpdated?: string;
  voicePresence?: string;
  userMuted?: string;
  userWarned?: string;
  threadCreated?: string;
  threadDeleted?: string;
  nicknameChanged?: string;
  roleChanged?: string;
  emojiUpdated?: string;
}

export interface EmbedField {
  name: string;
  value: string;
  inline: boolean;
}

export interface EmbedData {
  title?: string;
  description?: string;
  color?: string;
  author?: string;
  footer?: string;
  imageUrl?: string;
  thumbnailUrl?: string;
  fields?: EmbedField[];
}

export interface SavedEmbed {
  id: number;
  name: string;
  embedJson: string;
  creatorId: string;
}
