using System;

namespace Extended.Dapper.Tests.Models
{
    public static class ModelHelper
    {
        /// <summary>
        /// Generates an instance the chosen model
        /// </summary>
        /// <param name="modelType"></param>
        public static Author GetAuthorModel(AuthorModelType modelType)
        {
            switch (modelType)
            {
                case AuthorModelType.CarlSagan:
                    return new Author() {
                        Name = "Carl Sagan",
                        BirthYear = 1934,
                        Country = "United States"
                    };
                case AuthorModelType.StephenHawking:
                    return new Author() {
                        Name = "Stephen Hawking",
                        BirthYear = 1942,
                        Country = "United Kingdom"
                    };
            }

            return null;
        }

        /// <summary>
        /// Generates an instance of the chosen model, where you can include
        /// your own children; otherwise they will be generated
        /// </summary>
        /// <param name="modelType"></param>
        /// <param name="category"></param>
        /// <param name="author"></param>
        /// <param name="coAuthor"></param>
        /// <returns></returns>
        public static Book GetBookModel(BookModelType modelType, Category category = null, Author author = null, Author coAuthor = null)
        {
            switch (modelType)
            {
                case BookModelType.BriefAnswers:
                    return new Book() {
                        Name = "Brief Answers to the Big Questions",
                        ReleaseYear = 2018,
                        Author = author != null ? author : GetAuthorModel(AuthorModelType.StephenHawking),
                        Category = category != null ? category : GetScienceCategory()
                    };
                case BookModelType.BriefHistoryOfTime:
                    return new Book() {
                        Name = "A Brief History of Time",
                        ReleaseYear = 1988,
                        Author = author != null ? author : GetAuthorModel(AuthorModelType.StephenHawking),
                        Category = category != null ? category : GetScienceCategory()
                    };
                case BookModelType.Cosmos:
                    return new Book() {
                        Name = "Cosmos: A Personal Voyage",
                        ReleaseYear = 1980,
                        Author = author != null ? author : GetAuthorModel(AuthorModelType.CarlSagan)
                    };
                case BookModelType.PaleBlueDot:
                    return new Book() {
                        Name = "Pale Blue Dot: A Vision of the Human Future in Space",
                        ReleaseYear = 1994,
                        Author = author != null ? author : GetAuthorModel(AuthorModelType.CarlSagan),
                        Category = category != null ? category : GetScienceCategory()
                    };
                case BookModelType.ScienceAnswered:
                    return new Book() {
                        Name = "Science questions answered",
                        ReleaseYear = 2015,
                        Author = author != null ? author : GetAuthorModel(AuthorModelType.StephenHawking),
                        CoAuthor = coAuthor != null ? coAuthor : GetAuthorModel(AuthorModelType.CarlSagan),
                        Category = category != null ? category : GetScienceCategory()
                    };
            }

            return null;
        }

        /// <summary>
        /// Generates the science category
        /// </summary>
        public static Category GetScienceCategory() => new Category() {
            Name = "Science",
            Description = "All kinds of books with science"
        };

        /// <summary>
        /// Gets the author from a book model
        /// </summary>
        /// <param name="modelType"></param>
        public static AuthorModelType GetAuthorModelFromBookModel(BookModelType modelType)
        {
            switch (modelType)
            {
                case BookModelType.BriefAnswers:
                case BookModelType.BriefHistoryOfTime:
                case BookModelType.ScienceAnswered:
                    return AuthorModelType.StephenHawking;
                default:
                    return AuthorModelType.CarlSagan;
            }
        }
    }

    public enum AuthorModelType
    {
        CarlSagan,
        StephenHawking
    }

    public enum BookModelType
    {
        BriefAnswers,
        BriefHistoryOfTime,
        Cosmos,
        PaleBlueDot,
        ScienceAnswered
    }
}