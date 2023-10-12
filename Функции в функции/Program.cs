﻿using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Функции_в_функции
{
    internal class Program
    {
        static void Main(string[] args)
        {
        }
    }

    class Model
    {
        public bool CanVote(DataTable dataTable)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            if (dataTable.Rows.Count == 0)
                throw new InvalidDataException(nameof(dataTable.Rows));

            return Convert.ToBoolean(dataTable.Rows[0].ItemArray[1]);
        }
    }

    interface IView
    {
        void ShowMessage(string message);

        void ShowResult(string result);
    }

    class View : IView
    {
        private readonly Presenter _presenter;

        public View()
        {
            _presenter = new Presenter(this);
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        public void ShowResult(string result)
        {
            _textResult.Text = result;
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            _presenter.Process(_passportTextbox.Text);
        }
    }

    class Presenter
    {
        private readonly IView _view;
        private readonly DataBase _dataBase;
        private readonly Model _model;

        public Presenter(IView view)
        {
            _view = view;
        }

        public void Process(string passportData)
        {
            try
            {
                Passport passport = new Passport(passportData);

                DataTable dataTable = _dataBase.GetDataTable(passport);

                if (_model.CanVote(dataTable))
                {
                    _view.ShowResult($"По паспорту «{passport.Number}» доступ к бюллетеню на дистанционном электронном голосовании ПРЕДОСТАВЛЕН");
                }
                else
                {
                    _view.ShowResult($"По паспорту «{passport.Number}» доступ к бюллетеню на дистанционном электронном голосовании НЕ ПРЕДОСТАВЛЯЛСЯ");
                }
            }
            catch (SQLiteException exception)
            {
                if (ex.ErrorCode != 1)
                    return;

                _view.ShowMessage("Файл db.sqlite не найден. Положите файл в папку вместе с exe.");
            }
            catch (InvalidDataException exception)
            {
                _view.ShowResult($"Паспорт «{passportData}» в списке участников дистанционного голосования НЕ НАЙДЕН");
            }
            catch (ArgumentOutOfRangeException exception)
            {
                _view.ShowResult("Неверный формат серии или номера паспорта");
            }
            catch (ArgumentException exception)
            {
                _view.ShowMessage("Введите серию и номер паспорта");
            }
        }
    }

    class DataBase
    {
        public DataTable GetDataTable(Passport pasport)
        {
            string commandText = string.Format("select * from passports where num='{0}' limit 1;", (object)Form1.ComputeSha256Hash(pasport.Number));
            string connectionString = string.Format("Data Source=" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\db.sqlite");

            SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            SQLiteDataAdapter sqLiteDataAdapter = new SQLiteDataAdapter(new SQLiteCommand(commandText, connection));
            DataTable dataTable = new DataTable();
            sqLiteDataAdapter.Fill(dataTable);
            connection.Close();

            return dataTable;
        }
    }

    class Passport
    {
        public Passport(string passportData)
        {
            if (passportData == null)
                throw new ArgumentNullException(nameof(passportData));

            if (passportData == "")
                throw new ArgumentException(nameof(passportData));

            if (passportData.Length < 10)
                throw new ArgumentOutOfRangeException(nameof(passportData));

            Number = passportData;
        }

        public string Number { get; private set; }
    }
}

