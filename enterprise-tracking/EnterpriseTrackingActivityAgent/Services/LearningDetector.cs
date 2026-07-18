using System;
using System.Linq;

namespace EnterpriseTrackingActivityAgent.Services
{
    public class LearningDetector
    {
        static readonly string[] learningCourse =
        {
            "ai boost bites: your edge in the ai-powered world",
            "land your next job",
            "effective networking",
            "business communication",
            "promote a business with content",
            "speaking in public",
            "intro to digital well-being",
            "how to increase productivity at work",
            "understand the basics of code",
            "communicate your ideas through storytelling and design",
            "build confidence with self-promotion",
            "improve your online business security",
            "fundamentals of digital marketing",
            "connect with customers over mobile",
            "get started with google workspace tools",
            "elements of ai",
            "ai boost bites: your edge in the ai-powered world",
            "understand customers needs and online behaviours"
        };

        public bool IsLearningCourse(string windowTitle)
        {
            if (string.IsNullOrWhiteSpace(windowTitle))
                return false;

            var title = windowTitle.ToLower();

            return learningCourse.Any(course => title.Contains(course));
        }
    }
}