using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Views;
using Android.Widget;

using SQLite;

namespace Dibbi
{
    /*
    Acceso a datos.
    */
    public class DibbiData
    {
        // Conexión a base de datos SQLite.
        private SQLiteAsyncConnection Db = null;

        public DibbiData(string dbPath)
        {
            Initialize(dbPath);
        }

        private async void Initialize(string dbPath)
        {
            Db = new SQLiteAsyncConnection(dbPath);

            // Crea tabla solo si no existe.
            await Db.CreateTableAsync<NoteItem>();
        }

        public async Task<NoteItem> addNote(NoteItem note)
        {
            note.Id = await Db.InsertAsync(note);
            return note;
        }

        public async void removeNote(NoteItem note)
        {
            File.Delete(note.Filename);
            await Db.DeleteAsync(note);
        }

        public async Task<List<NoteItem>> GetNotes(Nullable<DateTime> last = null)
        {
            string query;

            // Cargar notas desde la ultima nota cargada.
            if (last.HasValue)
            {
                query = "SELECT * FROM notes WHERE created_at < ? ORDER BY created_at DESC LIMIT 20";
                return await Db.QueryAsync<NoteItem>(query, last.Value);
            }
            else
            {
                query = "SELECT * FROM notes ORDER BY created_at DESC LIMIT 20";
                return await Db.QueryAsync<NoteItem>(query);
            }
        }
    }

    [Table("Notes")]
    public class NoteItem
    {
        [PrimaryKey, AutoIncrement, Column("_id")]
        public int Id { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [MaxLength(80), Column("description")]
        public string Description { get; set; }
        [Column("duration")]
        public string Duration { get; set; }
        [Column("filename")]
        public string Filename { get; set; }
    }

    public class NoteListAdapter : BaseAdapter<NoteItem>
    {
        public List<NoteItem> items;
        public Activity context;

        public NoteListAdapter(Activity context, List<NoteItem> items) : base()
        {
            this.context = context;
            this.items = items;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override NoteItem this[int position]
        {
            get { return items[position]; }
        }

        public override int Count
        {
            get { return items.Count; }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            NoteItem item = items[position];
            View view = convertView;

            if (view == null)
                view = context.LayoutInflater.Inflate(Resource.Layout.MainListItem, null);

            view.FindViewById<TextView>(Resource.Id.ItemDescription).Text = item.Description;
            view.FindViewById<TextView>(Resource.Id.ItemDate).Text = item.CreatedAt.ToShortDateString();
            view.FindViewById<TextView>(Resource.Id.ItemDuration).Text = item.Duration;
            return view;
        }
    }
}