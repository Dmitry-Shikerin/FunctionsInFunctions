using Microsoft.SqlServer.Server;
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
            Database database = new Database();
            VoteSystem voteSystem = new VoteSystem();

            PresenterFactory presenterFactory = new PresenterFactory(database, voteSystem);

            IView view = new View(presenterFactory);
        }
    }

    class VoteSystem
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

        public View(PresenterFactory presenterFactory)
        {
            _presenter = presenterFactory.Create(this);
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

    class PresenterFactory
    {
        private readonly Database _database;
        private readonly VoteSystem _voteSystem;

        public PresenterFactory(Database database, VoteSystem voteSystem)
        {
            _database = database;
            _voteSystem = voteSystem;
        }

        public Presenter Create(IView view)
        {
            return new Presenter(view, _database, _voteSystem);
        }
    }

    class Presenter
    {
        private const int NumberErrorCode = 1;

        private readonly IView _view;
        private readonly Database _database;
        private readonly VoteSystem _voteSystem;

        public Presenter(IView view, Database database, VoteSystem voteSystem)
        {
            _view = view;
            _database = database;
            _voteSystem = voteSystem;
        }

        public void Process(string passportData)
        {
            Passport passport = null;

            try
            {
                passport = new Passport(passportData);

                DataTable dataTable = _database.GetDataTable(passport);

                if (_voteSystem.CanVote(dataTable))
                {
                    _view.ShowResult($"По паспорту «{passport.Number}» " +
                        $"доступ к бюллетеню на дистанционном электронном голосовании ПРЕДОСТАВЛЕН");
                }
                else
                {
                    _view.ShowResult($"По паспорту «{passport.Number}» " +
                        $"доступ к бюллетеню на дистанционном электронном голосовании НЕ ПРЕДОСТАВЛЯЛСЯ");
                }
            }
            catch (SQLiteException exception)
            {
                if (exception.ErrorCode != NumberErrorCode)
                    return;

                _view.ShowMessage("Файл db.sqlite не найден. Положите файл в папку вместе с exe.");
            }
            catch (InvalidDataException exception) when (passport != null)
            {
                _view.ShowResult($"Паспорт «{passport.Number}» в " +
                    $"списке участников дистанционного голосования НЕ НАЙДЕН");
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

    class Database
    {
        public DataTable GetDataTable(Passport pasport)
        {
            string commandText = string.Format("select * " +
                "from passports where num='{0}' limit 1;", (object)Form1.ComputeSha256Hash(pasport.Number));
            string connectionString = string.Format("Data Source=" + 
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\db.sqlite");

            SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            SQLiteDataAdapter sqLiteDataAdapter = 
                new SQLiteDataAdapter(new SQLiteCommand(commandText, connection));
            DataTable dataTable = new DataTable();
            sqLiteDataAdapter.Fill(dataTable);
            connection.Close();

            return dataTable;
        }
    }

    class Passport
    {
        private const int PassportDataLength = 10;

        public Passport(string passportData)
        {
            if (string.IsNullOrEmpty(passportData))
                throw new ArgumentException(nameof(passportData));

            if (passportData.Length < PassportDataLength)
                throw new ArgumentOutOfRangeException(nameof(passportData));

            Number = passportData;
        }

        public string Number { get; }
    }
}

