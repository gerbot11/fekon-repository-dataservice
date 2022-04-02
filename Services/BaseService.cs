﻿using fekon_repository_datamodel.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fekon_repository_dataservice.Services
{
    public class BaseService
    {
        public readonly REPOSITORY_DEVContext _context;
        public BaseService(REPOSITORY_DEVContext context)
        {
            _context = context;
        }

        public static DataTable GetDataBySp(string conString, string spName)
        {
            using MySqlConnection con = new(conString);

            if (con.State == ConnectionState.Closed) 
                con.Open();
            
            DataTable dt = new();
            using (MySqlCommand cmd = new(spName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using MySqlDataAdapter sda = new(cmd);
                sda.Fill(dt);
            }
            con.Close();

            return dt;
        }

        public static void SetParamDateBetweenYear(int yearStart, int yearTo, out DateTime dtStart, out DateTime dtTo)
        {
            dtStart = new(yearStart, 1, 1);

            if (yearTo is 0)
                dtTo = new(DateTime.Now.Year, 12, 31);
            else 
                dtTo = new(yearTo, 12, 31);
        }
    }
}