﻿using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeCare.Dto;
using WeCare.Util;
using WeCare.Persistance;
using AutoMapper;
using WeCare.Entities;

namespace WeCare.Service.Impl
{
    public class EventServiceImpl : EventService
    {
        private readonly ApplicationDbContext pContext;
        private readonly IMapper pMapper;
        private static int pid;
        public EventServiceImpl(ApplicationDbContext context, IMapper mapper)
        {
            pid = 0;
            pContext = context;
            pMapper = mapper;
        }
        public EventDto Create(EventCreateDto model)
        {
            Patient patient = pContext.Patients.Single(x => x.PatientId == model.PatientId);
            var entry = new Event
            {
                EventName = model.EventName,
                EventDescription = model.EventDescription,
                EventScore = model.EventScore,
                EventResult = model.EventResult,
                EventDetail = model.EventDetail,
                EventDate = DateTime.Now.ToString("yyyy-MM-dd"),
                EventTime = DateTime.Now.ToString("hh:mm:ss"),
                PatientId = model.PatientId,
                Patient = patient,
                EventId = pid++
            };
            pContext.Events.Add(entry);
            pContext.SaveChanges();
            return pMapper.Map<EventDto>(entry);
        }

        public DataCollection<EventDto> GetAll(int page, int take)
        {
            return pMapper.Map<DataCollection<EventDto>>(pContext.Events.Include(x => x.Patient).
                OrderByDescending(x => x.EventId).AsQueryable().Paged(page, take)
                );
        }

        public DataCollection<EventSimpleDto> GetAllSimple(int page, int take)
        {
            return pMapper.Map<DataCollection<EventSimpleDto>>(pContext.Events.
                OrderByDescending(x => x.EventId).AsQueryable().Paged(page, take)
                );
        }

        public EventDto GetById(int eventId)
        {
            return pMapper.Map<EventDto>(pContext.Events.
                Single(x => x.EventId == eventId));
        }

        public DataCollection<EventSimpleDto> GetSimpleByPatientId(int patientId, int page, int take)
        {
            return pMapper.Map<DataCollection<EventSimpleDto>>(pContext.Events.
                Where(x => x.PatientId == patientId).
                OrderByDescending(x => x.EventId).
                AsQueryable().Paged(page, take)
                );
        }

        public EventDto CreateSimple(EventSimpleCreateDto model)
        {
            Patient patient = pContext.Patients.Single(x => x.PatientId == model.PatientId);
            var entry = new Event
            {
                EventName = DateTime.Now.ToString("yyyy-MM-dd") + " " +
                            DateTime.Now.ToString("HH:mm:ss"),
                EventDescription = "No description",
                EventScore = model.EventScore,
                EventResult = "Pending",
                EventDetail = "No details",
                EventDate = DateTime.Now.ToString("yyyy-MM-dd"),
                EventTime = DateTime.Now.ToString("HH:mm:ss"),
                PatientId = model.PatientId,
                Patient = patient,
                EventId = pid++
            };
            pContext.Events.Add(entry);
            pContext.SaveChanges();
            return pMapper.Map<EventDto>(entry);
        }

        public List<EventSimpleDto> GetTodayEvents(int patientId, int page, int take)
        {
            List<EventSimpleDto> fool = new List<EventSimpleDto>();
            var lis = DataEvents(patientId, page, take);
            foreach (var ele in lis.Items) if (checkDay(ele.EventDate,DateTime.Now)) fool.Add(ele);
            return fool;
        }

        public List<(string, int)> GetWeeklyEvents(int patientId, int page, int take)
        {
            // Primero, obtener el dia de inicio de semana y calcular desde alli un rango
            var SoW = getStartWeek(DateTime.Now.ToString());
            var EoW = getEndWeek(SoW);
            //var EoW = DateTime.Now;
            List<EventSimpleDto> fool = new List<EventSimpleDto>();
            var lis = DataEvents(patientId, page, take);
            foreach (var ele in lis.Items)
            {
                if (checkWeek(ele.EventDate, SoW, EoW)) {
                    fool.Add(ele);
                }
            }
            List<(string, int)> dateproms = new List<(string, int)>();
            
            foreach (var dum in fool.Select(x => x.EventDate))
            {
                var promday = 0; var count = 0;
                foreach (var foli in fool)
                {
                    if (foli.EventDate == dum) promday += foli.EventScore; count++;
                }
                dateproms.Add((dum,promday/count));
            }
            return dateproms.Distinct().ToList();
            //return dateproms.Distinct().ToList();
        }

        public List<(string, int)> GetMonthlyEvents(int patientId, int page, int take)
        {
            List<(string, int)> weekproms = new List<(string, int)>();
            var count = 0;
            var evaluating = new DateTime(); 
            var ids = new DateTime();
            var fds = new DateTime();
            var year = DateTime.Now.Year;
            var mon = DateTime.Now.Month;
            for (var tempdate = 1; tempdate <= DateTime.Now.Day; tempdate += 7)
            {
                count++;
                evaluating = new DateTime(year, mon, tempdate);
                if (tempdate == 1)
                {
                    ids = new DateTime(year, mon, tempdate);
                    fds = getEndWeek(getStartWeek(evaluating.ToString()));
                }
                else
                {
                    ids = getStartWeek(evaluating.ToString());
                    fds = getEndWeek(ids);
                }
                var dum = WeekEvents(patientId, ids, fds, page, take);
                weekproms.Add(("Week " + count, dum));
            }
            return weekproms;
        }

        private bool checkDay(string EventDate, DateTime compare)
        {
            var eDay = DateTime.Parse(EventDate);
            if (eDay.Day == compare.Day && eDay.Month == compare.Month &&
                eDay.Year == compare.Year) return true;
            return false;
        }
        // Para sacar el inicio de semana
        private DateTime getStartWeek(string EventDate)
        {
            var eDay = DateTime.Parse(EventDate);
            var startweek = new DateTime();
            if (eDay.DayOfWeek == DayOfWeek.Monday) 
                startweek = new DateTime(eDay.Year,eDay.Month,eDay.Day);
            else
            {
                var DoW = (int)eDay.DayOfWeek;
                if (DoW == 0) DoW = 7;
                var tempdate = new DateTime();
                var difDay = 0;
                if (eDay.Day > DoW) difDay = Math.Abs(1 - DoW);
                else difDay = Math.Abs(DoW - 1);
                tempdate = eDay.AddDays(-difDay);
                startweek = new DateTime(tempdate.Year, tempdate.Month, tempdate.Day);
            }
            return startweek;
        }
        private DateTime getEndWeek(DateTime startWeek)
        {
            var endWeek = startWeek.AddDays(6);
            return new DateTime(endWeek.Year, endWeek.Month, endWeek.Day, 23, 59, 59);
        }
        private bool checkWeek(string EventDate, DateTime startWeek, DateTime endWeek)
        {
            var eDay = DateTime.Parse(EventDate);
            if (eDay >= startWeek && eDay <= endWeek) { 
                return true; 
            }
            else return false;
        }
        //
        private DataCollection<EventSimpleDto> DataEvents(int patientId, int page, int take)
        {
            return pMapper.Map<DataCollection<EventSimpleDto>>(pContext.Events.
                Where(x => x.PatientId == patientId).
                OrderByDescending(x => x.EventId).
                AsQueryable().Paged(page, take)
                );
        }
        private List<EventSimpleDto> DataMonthly(int patientId, int page, int take)
        {
            List<EventSimpleDto> fool = new List<EventSimpleDto>();
            var lis = DataEvents(patientId, page, take);
            foreach (var ele in lis.Items)
            {
                var eMon = DateTime.Parse(ele.EventDate);
                if (eMon.Year == DateTime.Now.Year && eMon.Month == DateTime.Now.Month)
                {
                    fool.Add(ele);
                }
            }
            return fool;
        }
        private int WeekEvents(int patientId, DateTime StartWeek, DateTime EndWeek, int page, int take)
        {
            List<EventSimpleDto> foo = new List<EventSimpleDto>();
            var loo = DataMonthly(patientId, page, take);
            foreach (var ele in loo)
            {
                var eDay = DateTime.Parse(ele.EventDate);
                if (eDay >= StartWeek && eDay <= EndWeek)
                {
                    foo.Add(ele);
                }
            }
            if (foo.Count > 0)
            {
                List<(string, int)> dateproms = new List<(string, int)>();
                foreach (var dum in foo.Select(x => x.EventDate))
                {
                    var promday = 0; var count = 0;
                    foreach (var foli in foo)
                    {
                        if (foli.EventDate == dum) promday += foli.EventScore; count++;
                    }
                    dateproms.Add((dum, promday / count));
                }
                var promsem = 0; var countsem = 0;
                foreach (var s in dateproms)
                {
                    countsem++;
                    promsem += s.Item2;
                }
                return promsem / countsem;
            }
            else return 0;
        }
    }

}
