using AutoMapper;
using Miniblog.Core.Models;

namespace Miniblog.Core.Profiles
{
    // ReSharper disable once UnusedMember.Global
    public class PostProfile : Profile
    {
        public PostProfile()
        {
            CreateMap<Post, PostBase>()
                .ReverseMap();
        }
    }
}
