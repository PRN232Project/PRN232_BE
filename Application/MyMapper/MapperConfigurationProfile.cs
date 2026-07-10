using AutoMapper;
using OnlineLearningPlatformApi.Application.Requests.Course;
using OnlineLearningPlatformApi.Application.Requests.Enrollment;
using OnlineLearningPlatformApi.Application.Requests.Lesson;
using OnlineLearningPlatformApi.Application.Requests.LessonResource;
using OnlineLearningPlatformApi.Application.Requests.Module;
using OnlineLearningPlatformApi.Application.Responses.Course;
using OnlineLearningPlatformApi.Application.Responses.Lesson;
using OnlineLearningPlatformApi.Application.Responses.LessonItem;
using OnlineLearningPlatformApi.Application.Responses.LessonResource;
using OnlineLearningPlatformApi.Application.Responses.Module;
using OnlineLearningPlatformApi.Application.Responses.User;
using OnlineLearningPlatformApi.Domain.Entities;

namespace OnlineLearningPlatformApi.Application.MyMapper
{
    public class MapperConfigurationProfile : Profile
    {
        public MapperConfigurationProfile()
        {
            //User
            CreateMap<User, ProfileResponse>();
            //Enrollment
            CreateMap<CreateNewEnrollementRequest, Enrollment>();

            //Course
            CreateMap<CreateNewCourseRequest, Course>();
            CreateMap<Course, CourseResponse>();
            CreateMap<UpdateCourseRequest, Course>();
            CreateMap<Course, GetAllCourseForAdminResponse>();
            CreateMap<Course, StudentCourseDetailResponse>();

            //Lesson
            CreateMap<CreateNewLessonForModuleRequest, Lesson>()
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Content));
            CreateMap<UpdateLessonRequest, Lesson>();
            CreateMap<Lesson, LessonResponse>()
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Description));
            CreateMap<Lesson, LessonDetailResponse>();
            CreateMap<LessonItem, LessonItemResponse>();

            //LessonResource
            CreateMap<CreateLessonResourceRequest, LessonResource>();
            CreateMap<LessonResource, LessonResourceResponse>();

            //Module
            CreateMap<CreateNewModuleForCourseRequest, Module>();
            CreateMap<UpdateModuleRequest, Module>();
            CreateMap<Module, ModuleResponse>();

            CreateMap<Course, CourseEditSummaryResponse>();

            CreateMap<OnlineLearningPlatformApi.Domain.Entities.Module, OnlineLearningPlatformApi.Application.Responses.Course.CourseModuleEditResponse>().ReverseMap();

            CreateMap<OnlineLearningPlatformApi.Domain.Entities.Lesson, OnlineLearningPlatformApi.Application.Responses.Course.CourseLessonEditResponse>().ReverseMap();

            CreateMap<OnlineLearningPlatformApi.Domain.Entities.LessonItem, OnlineLearningPlatformApi.Application.Responses.Course.CourseLessonItemEditResponse>().ReverseMap();

            CreateMap<OnlineLearningPlatformApi.Domain.Entities.LessonResource, OnlineLearningPlatformApi.Application.Responses.Course.CourseLessonResourceEditResponse>().ReverseMap();

            CreateMap<OnlineLearningPlatformApi.Domain.Entities.AnswerOption, OnlineLearningPlatformApi.Application.Responses.Course.CourseAnswerOptionEditResponse>().ReverseMap();
        }
    }
}
