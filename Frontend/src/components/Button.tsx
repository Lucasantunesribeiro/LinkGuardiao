import React from 'react';
import { Link } from 'react-router-dom';

type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  href?: string;
  children: React.ReactNode;
};

export const Button: React.FC<ButtonProps> = ({ href, children, className = '', ...props }) => {
  if (href) {
    return (
      <Link to={href} className={`btn-primary px-4 py-2 rounded font-semibold ${className}`}>
        {children}
      </Link>
    );
  }
  return (
    <button className={`btn-primary px-4 py-2 rounded font-semibold ${className}`} {...props}>
      {children}
    </button>
  );
}; 