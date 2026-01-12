export interface UserDto {
  id: number;
  username: string;
  email: string;
}

export interface AuthResult {
  success?: boolean;
  message?: string;
  token: string;
  user: UserDto;
}

export interface ShortenedLink {
  id: number;
  originalUrl: string;
  shortCode: string;
  title?: string | null;
  createdAt: string;
  expiresAt?: string | null;
  isActive: boolean;
  isPasswordProtected?: boolean;
}

export interface BrowserStatsDto {
  browser: string;
  count: number;
  percentage: number;
}

export interface IpStatsDto {
  ipAddress: string;
  count: number;
  percentage: number;
}

export interface DateStatsDto {
  date: string;
  count: number;
}

export interface LinkStatsDto {
  linkId: number;
  shortCode: string;
  originalUrl: string;
  totalClicks: number;
  browserStats: BrowserStatsDto[];
  topIpAddresses: IpStatsDto[];
  clicksByDate: DateStatsDto[];
}

export interface LinkCreateRequest {
  originalUrl: string;
  title?: string | null;
  password?: string | null;
  expiresAt?: string | null;
}
