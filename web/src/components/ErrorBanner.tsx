interface Props {
  message: string;
  onRetry: () => void;
}

export function ErrorBanner({ message, onRetry }: Props) {
  return (
    <div className="banner" role="alert">
      <span className="banner__text">⚠ {message}</span>
      <button className="btn btn--small" onClick={onRetry}>
        重试
      </button>
    </div>
  );
}
