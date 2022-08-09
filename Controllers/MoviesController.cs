using ASPNET5CRUD.Models;
using ASPNET5CRUD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ASPNET5CRUD.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _nToastNotify;
        private readonly List<string> _allowedPosterExtensions = new List<string>() { ".jpg", ".png" };
        private readonly long _maxAllowedPosterSize = 1048576;

        public MoviesController(ApplicationDbContext context, IToastNotification nToastNotify)
        {
            _context = context;
            _nToastNotify = nToastNotify;
        }

        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies.OrderByDescending(x => x.Rate).ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Create()
        {
            var MovieViewModel = new MovieFormViewModel
            {
                Genres = await _context.Genres.ToListAsync()
            };
            return View("MovieForm", MovieViewModel);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.ToListAsync();
                return View("MovieForm", model);
            }

            var files = Request.Form.Files;

            if (!files.Any())
            {
                model.Genres = await _context.Genres.ToListAsync();
                ModelState.AddModelError("Poster", "Please select a movie poster");
                return View("MovieForm", model);
            }

            var poster = files.First();
            string posterCheckerStr = string.Empty;
            bool posterchecker = CheckPosterFile(poster, out posterCheckerStr);

            if (!posterchecker)
            {
                model.Genres = await _context.Genres.ToListAsync();
                ModelState.AddModelError("Poster", posterCheckerStr);
                return View("MovieForm", model);
            }

            using var dataStream = new MemoryStream();
            await poster.CopyToAsync(dataStream);

            var movie = new Movie()
            {
                Title = model.Title,
                Year = model.Year,
                Rate = model.Rate,
                StoryLine = model.StoryLine,
                GenreId = model.GenreId,
                Poster = dataStream.ToArray()
            };

            _context.Movies.Add(movie);
            _context.SaveChanges();

            _nToastNotify.AddSuccessToastMessage("Movie saved succefully");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue)
                return BadRequest();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound();

            var MovieViewModel = new MovieFormViewModel
            {
                Genres = await _context.Genres.ToListAsync(),
                Id = movie.Id,
                GenreId = movie.GenreId,
                Year = movie.Year,
                Rate = movie.Rate,
                StoryLine = movie.StoryLine,
                Title = movie.Title,
                Poster = movie.Poster
            };
            return View("MovieForm", MovieViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.ToListAsync();
                return View("MovieForm", model);
            }

            var movie = await _context.Movies.FindAsync(model.Id);
            if (movie == null)
                return NotFound();


            /******Poster******/
            var files = Request.Form.Files;

            if (files.Any())
            {
                var poster = files.First();


                using var datastream = new MemoryStream();
                await poster.CopyToAsync(datastream);
                model.Poster = datastream.ToArray();

                string posterCheckerStr = string.Empty;
                bool posterchecker = CheckPosterFile(poster, out posterCheckerStr);

                if (!posterchecker)
                {
                    model.Genres = await _context.Genres.ToListAsync();
                    ModelState.AddModelError("Poster", posterCheckerStr);
                    return View("MovieForm", model);
                }

                movie.Poster = model.Poster;
            }

            /************/

            movie.Title = model.Title;
            movie.Year = model.Year;
            movie.Rate = model.Rate;
            movie.StoryLine = model.StoryLine;
            movie.GenreId = model.GenreId;

            _context.SaveChanges();

            _nToastNotify.AddSuccessToastMessage("Movie updated succefully");
            return RedirectToAction(nameof(Index));

        }

        public async Task<IActionResult> Details(int? id) 
        {
            if (!id.HasValue)
                return BadRequest();

            var movie = await _context.Movies.Include(x  => x.Genre).SingleOrDefaultAsync(x => x.Id == id);

            if (movie == null)
                return NotFound();

            return View(movie);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue)
                return BadRequest();

            var movie = await _context.Movies.Include(x => x.Genre).SingleOrDefaultAsync(x => x.Id == id);

            if (movie == null)
                return NotFound();

            _context.Movies.Remove(movie);
            _context.SaveChanges();

            return Ok();
        }

        #region HelperFunctions
        private bool CheckPosterFile(IFormFile poster, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (!_allowedPosterExtensions.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {
                errorMsg = ".jpg , .png are the allowed extensions";
                return false;
            }

            //1 MB
            if (poster.Length > _maxAllowedPosterSize)
            {
                errorMsg = "Poster cannot be more than 1 MB!";
                return false;
            }

            return true;
        }

        #endregion
    }
}
