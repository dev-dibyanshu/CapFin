export const Card = ({ children, className = '', title, icon: Icon }) => {
  return (
    <div className={`bg-white dark:bg-slate-800 rounded-xl shadow-md p-6 transition-all duration-200 ${className}`}>
      {(title || Icon) && (
        <div className="flex items-center gap-3 mb-4">
          {Icon && <Icon className="h-6 w-6 text-primary-600 dark:text-primary-400" />}
          {title && <h3 className="text-lg font-semibold text-slate-900 dark:text-slate-100">{title}</h3>}
        </div>
      )}
      {children}
    </div>
  );
};
