import type { Status } from '../types';

const OPTIONS: { value: Status; label: string }[] = [
  { value: 'open', label: '待处理' },
  { value: 'in_progress', label: '处理中' },
  { value: 'resolved', label: '已解决' },
];

interface Props {
  value: Status;
  disabled?: boolean;
  onChange: (next: Status) => void;
}

export function StatusSelect({ value, disabled, onChange }: Props) {
  return (
    <select
      className={`status status--${value}`}
      value={value}
      disabled={disabled}
      onChange={e => onChange(e.target.value as Status)}
    >
      {OPTIONS.map(o => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  );
}
