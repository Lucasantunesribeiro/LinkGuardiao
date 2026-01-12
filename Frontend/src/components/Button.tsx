import React from 'react';
import { Link } from 'react-router-dom';

type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  href?: string;
  variant?: 'primary' | 'secondary' | 'ghost' | 'outline';
  size?: 'sm' | 'md' | 'lg';
  children: React.ReactNode;
};

const variantClasses: Record<NonNullable<ButtonProps['variant']>, string> = {
  primary: 'btn-primary',
  secondary: 'btn-secondary',
  ghost: 'btn-ghost',
  outline: 'btn-outline'
};

const sizeClasses: Record<NonNullable<ButtonProps['size']>, string> = {
  sm: 'btn-sm',
  md: '',
  lg: 'btn-lg'
};

export const Button: React.FC<ButtonProps> = ({
  href,
  variant = 'primary',
  size = 'md',
  children,
  className = '',
  ...props
}) => {
  const classes = `${variantClasses[variant]} ${sizeClasses[size]} ${className}`.trim();
  if (href) {
    return (
      <Link to={href} className={classes}>
        {children}
      </Link>
    );
  }
  return (
    <button className={classes} {...props}>
      {children}
    </button>
  );
};
