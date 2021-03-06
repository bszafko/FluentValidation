#region License
// Copyright 2008-2009 Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at http://www.codeplex.com/FluentValidation
#endregion

namespace FluentValidation {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using Internal;
	using Results;
	using Validators;

	/// <summary>
	/// Base class for entity validator classes.
	/// </summary>
	/// <typeparam name="T">The type of the object being validated</typeparam>
	public abstract class AbstractValidator<T> : IValidator<T>, IEnumerable<IValidationRule<T>> {
		readonly List<IValidationRuleCollection<T>> nestedValidators = new List<IValidationRuleCollection<T>>();

		ValidationResult IValidator.Validate(object instance) {
			//TODO: Type checking
			return Validate((T)instance);
		}

		/// <summary>
		/// Validates the specified instance
		/// </summary>
		/// <param name="instance">The object to validate</param>
		/// <returns>A ValidationResult object containing any validation failures</returns>
		public virtual ValidationResult Validate(T instance) {
			return Validate(new ValidationContext<T>(instance, new PropertyChain(), new DefaultValidatorSelector()));
		}
		
		/// <summary>
		/// Validates the specified instance.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <returns>A ValidationResult object containing any validation failures.</returns>
		public virtual ValidationResult Validate(ValidationContext<T> context) {
			context.Guard("Cannot pass null to Validate");
			var failures = nestedValidators.SelectMany(x => x.Validate(context)).ToList();
			return new ValidationResult(failures);
		}

		public void AddRule(IValidationRule<T> rule) {
			nestedValidators.Add(new SimpleRuleBuilder<T>(rule));
		}

		public virtual IValidatorDescriptor CreateDescriptor() {
			return new ValidatorDescriptor<T>(nestedValidators.SelectMany(x => x).ToList());
		}

		/// <summary>
		/// Defines a validation rule for a specify property.
		/// </summary>
		/// <example>
		/// RuleFor(x => x.Surname)...
		/// </example>
		/// <typeparam name="TProperty">The type of property being validated</typeparam>
		/// <param name="expression">The expression representing the property to validate</param>
		/// <returns>an IRuleBuilder instance on which validators can be defined</returns>
		public IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression) {
			expression.Guard("Cannot pass null to RuleFor");
			var ruleBuilder = new RuleBuilder<T, TProperty>(expression);
			nestedValidators.Add(ruleBuilder);
			return ruleBuilder;
		}

		/// <summary>
		/// Defines a custom validation rule using a lambda expression.
		/// If the validation rule fails, it should return a instance of a <see cref="ValidationFailure">ValidationFailure</see>
		/// If the validation rule succeeds, it should return null.
		/// </summary>
		/// <param name="customValidator">A lambda that executes custom validation rules.</param>
		public void Custom(Func<T, ValidationFailure> customValidator) {
			customValidator.Guard("Cannot pass null to Custom");
			AddRule(new DelegateValidator<T>(x => new[] { customValidator(x) }));
		}

		/// <summary>
		/// Defines a custom validation rule using a lambda expression.
		/// If the validation rule fails, it should return an instance of <see cref="ValidationFailure">ValidationFailure</see>
		/// If the validation rule succeeds, it should return null.
		/// </summary>
		/// <param name="customValidator">A lambda that executes custom validation rules</param>
		public void Custom(Func<T, ValidationContext<T>, ValidationFailure> customValidator) {
			customValidator.Guard("Cannot pass null to Custom");
			AddRule(new DelegateValidator<T>((x, ctx) => new[] { customValidator(x, ctx) }));
		}

		public IEnumerator<IValidationRule<T>> GetEnumerator() {
			return nestedValidators.SelectMany(x => x).ToList().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}