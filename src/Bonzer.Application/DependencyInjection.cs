using Bonzer.Application.Features.AcademicYears;
using Bonzer.Application.Features.Departments;
using Bonzer.Application.Features.Employees;
using Bonzer.Application.Features.Students;
using Microsoft.Extensions.DependencyInjection;

namespace Bonzer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddSingleton(TimeProvider.System);

        // One service per feature module. Add new modules here (Attendance, Exams, Fees...).
        services.AddScoped<IAcademicYearService, AcademicYearService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();

        return services;
    }
}
