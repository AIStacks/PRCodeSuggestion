export type Result<T> = {
  isSuccess: boolean;
  errors: string[];
  value: T | null;
};
