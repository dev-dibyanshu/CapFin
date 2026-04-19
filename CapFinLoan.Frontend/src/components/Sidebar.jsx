import { Link, useLocation } from 'react-router-dom';
import { LayoutDashboard, FileText, Upload, Shield } from 'lucide-react';

export const Sidebar = ({ role }) => {
  const location = useLocation();
  
  const menuItems = [
    { path: '/dashboard', label: 'Dashboard', icon: LayoutDashboard, roles: ['APPLICANT', 'ADMIN'] },
    { path: '/application', label: 'Applications', icon: FileText, roles: ['APPLICANT'] },
    { path: '/documents', label: 'Upload Documents', icon: Upload, roles: ['APPLICANT'] },
    { path: '/admin', label: 'Admin Panel', icon: Shield, roles: ['ADMIN'] },
  ];

  const filteredItems = menuItems.filter(item => item.roles.includes(role));

  return (
    <aside className="w-64 bg-white dark:bg-slate-800 border-r border-slate-200 dark:border-slate-700 min-h-screen">
      <div className="p-6">
        <h1 className="text-2xl font-bold text-primary-600 dark:text-primary-400">CapFinLoan</h1>
        <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">Loan Management</p>
      </div>
      
      <nav className="px-4 space-y-2">
        {filteredItems.map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.path;
          
          return (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center gap-3 px-4 py-3 rounded-lg transition-all duration-200 ${
                isActive
                  ? 'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400 font-medium'
                  : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-700'
              }`}
            >
              <Icon className="h-5 w-5" />
              <span>{item.label}</span>
            </Link>
          );
        })}
      </nav>
    </aside>
  );
};
