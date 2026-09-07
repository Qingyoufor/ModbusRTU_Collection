using AutoMapper;
using Server.Models.Dtos;
using Server.Models.Entities;

namespace Server.Infrastructure.Mappings
{
    public class MappingProfile : Profile
    {
        // 注册 A→B 一个映射，单体/集合/嵌套集合也会自动映射
        // Entity, Dto => Read，返回给前端
        // Dto, Entity => Write,接收前端提交的数据并写入数据库(常用于创建和更新)
        // ForMember对目标对象的某个成员进行操作，MapFrom：从源对象的某个成员映射
        public MappingProfile()
        {
            // ====== Entity -> Dto 返回给前端 GET=====
            CreateMap<Device, DeviceDto>()
                .ForMember(dest => dest.ProductionLineName,
                opt => opt.MapFrom(src => src.ProductionLine != null ?
                src.ProductionLine.Name : null));

            CreateMap<Device,DeviceListItemDto>()
                .ForMember(dest => dest.ProductionLineName,
                opt => opt.MapFrom(src => src.ProductionLine != null ?
                src.ProductionLine.Name : null));

            CreateMap<DataPoint, DataPointDto>();

            CreateMap<ProductionLine, ProductionLineDto>()
                .ForMember(dest => dest.DeviceCount,
                opt => opt.MapFrom(src => src.Devices.Count));

            CreateMap<User, UserDto>();

            CreateMap<Alarm, AlarmDto>()
                .ForMember(dest => dest.DeviceName,
                opt => opt.MapFrom(src => src.Device != null ? src.Device.Name : null))
                .ForMember(dest => dest.DataPointName,
                opt => opt.MapFrom(src => src.DataPoint != null ? src.DataPoint.Name : null))
                .ForMember(dest => dest.Level,
                opt => opt.MapFrom(src => src.Level.ToString()))
                .ForMember(dest => dest.AlarmType,
                opt => opt.MapFrom(src => src.AlarmType.ToString()))
                .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.Status.ToString()));

            // ====== Request DTO → Entity （创建/更新时用）======
            CreateMap<CreateProductionLineRequest, ProductionLine>();
            CreateMap<UpdateProductionLineRequest, ProductionLine>();

            CreateMap<CreateDeviceRequest, Device>();
            CreateMap<UpdateDeviceRequest, Device>();

            CreateMap<CreateDataPointRequest, DataPoint>();
            CreateMap<UpdateDataPointRequest, DataPoint>();

        }
    }
}
