namespace Bonzer.Domain.Enums;

[Flags]
public enum NotificationChannel { None = 0, InApp = 1, Email = 2, Sms = 4, Push = 8, WhatsApp = 16 }

public enum NotificationCategory { General, Academic, Attendance, Exam, Fee, Homework, Event, Emergency }

public enum AudienceType { Everyone, Branch, ClassLevel, Section, Role, Individuals }

public enum DeliveryStatus { Pending, Sent, Delivered, Failed, Read }

public enum SubmissionStatus { Pending, Submitted, Late, Graded, Returned }
