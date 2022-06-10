﻿using AutoMapper;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeCare.Dto;
using WeCare.Entities;
using WeCare.Persistance;
using WeCare.Util;

namespace WeCare.Service.Impl
{
    public class PatientServiceImpl : PatientService
    {
        private readonly ApplicationDbContext pContext;
        private readonly IMapper pMapper;
        private static int pid;
        public PatientServiceImpl(ApplicationDbContext context, IMapper mapper)
        {
            pid = 0;
            pContext = context;
            pMapper = mapper;
        }
        public PatientDto Create(PatientCreateDto model)
        {           
            var entry = new Patient
            {
                PatientName = model.PatientName,
                PatientLastname = model.PatientLastname,
                PatientEmail = model.PatientEmail,
                SpecialistId = 1,
                Specialist = pContext.Specialists.Single(x => x.SpecialistId == 1),
                PatientId = pid++
            };
            pContext.Patients.Add(entry);
            pContext.SaveChanges();
            return pMapper.Map<PatientDto>(entry);
        }

        public DataCollection<PatientDto> GetAll(int page, int take)
        {
            return pMapper.Map<DataCollection<PatientDto>>(pContext.Patients.Include(x => x.Specialist).
                OrderByDescending(x => x.PatientId).AsQueryable().Paged(page, take)
                );
        }

        public DataCollection<PatientSimpleDto> GetAllSimple(int page, int take)
        {
            return pMapper.Map<DataCollection<PatientSimpleDto>>(pContext.Patients.
                OrderByDescending(x => x.PatientId).AsQueryable().Paged(page, take)
                );
        }

        public PatientDto GetByEmail(string patientEmail)
        {
            return pMapper.Map<PatientDto>(pContext.Patients.
                Single(x => x.PatientEmail == patientEmail));
        }

        public PatientDto GetById(int patientId)
        {
            return pMapper.Map<PatientDto>(pContext.Patients.
                Single(x => x.PatientId == patientId));
        }

        public DataCollection<PatientSimpleDto> GetSimpleBySpecialistId(int specialistId, int page, int take)
        {
            return pMapper.Map<DataCollection<PatientSimpleDto>>(pContext.Patients.
                Where(x => x.SpecialistId == specialistId).OrderByDescending(x => x.PatientId).
                AsQueryable().Paged(page, take)
                );
        }
    }
}
