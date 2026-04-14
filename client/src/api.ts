import axios from 'axios';

export const api = axios.create({ baseURL: '/api' });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export interface LoginResponse {
  token: string;
  email: string;
  roles: string[];
}

export interface DocumentDto {
  id: string;
  fileName: string;
  type: string;
  version: number;
  uploadedAt: string;
  status: string;
  allowedRoles: string[];
}

export interface DocumentTypeDto {
  id: string;
  name: string;
  displayName: string;
  isBuiltIn: boolean;
}

export interface SourceDto {
  documentId: string;
  chunkIndex: number;
  version: number;
  documentType: string;
  score: number;
  snippet: string;
}

export interface AskResponse {
  answer: string;
  sources: SourceDto[];
}

export interface ChatSourceDto {
  documentId: string;
  documentType: string;
  version: number;
  chunkIndex: number;
  score: number;
  snippet: string;
}

export interface ChatMessageDto {
  role: 'User' | 'Assistant';
  content: string;
  createdAt: string;
  sources: ChatSourceDto[];
}

export interface ChatSessionSummary {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
}

export interface ChatSession {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messages: ChatMessageDto[];
}
